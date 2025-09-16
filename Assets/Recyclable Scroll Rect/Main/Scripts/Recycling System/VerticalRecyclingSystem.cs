//MIT License
//Copyright (c) 2020 Mohammed Iqubal Hussain
//Website : Polyandcode.com 

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PolyAndCode.UI
{
	/// <summary>
	/// Recyling system for Vertical type.
	/// </summary>
	public class VerticalRecyclingSystem : RecyclingSystem
	{
		//Assigned by constructor
		private readonly int _coloumns;

		//Cell dimensions
		private float _cellWidth;

		//Pool Generation
		private List<RectTransform> _cellPool;
		private List<ICell> _cachedCells;
		private Bounds _recyclableViewBounds;

		//Temps, Flags 
		private readonly Vector3[] _corners = new Vector3[4];
		private bool _recycling;

		//Trackers
		private int _currentItemCount; //item count corresponding to the datasource.
		private int _topMostCellIndex, _bottomMostCellIndex; //Topmost and bottommost cell in the hierarchy

		private int
			_topMostCellColumn,
			_bottomMostCellColumn; // used for recycling in Grid layout. top-most and bottom-most column

		//Cached zero vector 
		private readonly Vector2 _zeroVector = Vector2.zero;
		
		private float _virtualTop;

		#region INIT

		public VerticalRecyclingSystem(RectTransform prototypeCell, RectTransform viewport, RectTransform content,
			IRecyclableScrollRectDataSource dataSource, bool isGrid, int columns)
		{
			PrototypeCell = prototypeCell;
			Viewport = viewport;
			Content = content;
			DataSource = dataSource;
			IsGrid = isGrid;
			_coloumns = isGrid ? columns : 1;
			_recyclableViewBounds = new Bounds();
		}
		
		/// <summary>
		/// Coroutine for initialization.
		/// Using coroutine for init because few UI stuff requires a frame to update
		/// </summary>
		/// <param name="onInitialized">callback when init done</param>
		/// <returns></returns>>
		public override IEnumerator InitCoroutine(System.Action onInitialized)
		{
			SetTopAnchor(Content);
			Content.anchoredPosition = Vector3.zero;
			yield return null;
			SetRecyclingBounds();

			//Cell Poool
			CreateCellPool();
			
			float windowH = RecalculateWindowHeight();
			Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, windowH);
			_virtualTop = 0f; // we start at the very top of the dataset
			
			_currentItemCount = _cellPool.Count;
			_topMostCellIndex = 0;
			_bottomMostCellIndex = _cellPool.Count - 1;

			//Set content height according to no of rows
			int noOfRows = (int)Mathf.Ceil((float)_cellPool.Count / (float)_coloumns);
			
			float contentYSize = 0f;
			
			for (int i = 0; i < noOfRows; i++)
			{
				contentYSize += DataSource.GetHeight(i);
			}
			
			Content.sizeDelta = new Vector2(Content.sizeDelta.x, contentYSize);
			SetTopAnchor(Content);

			if (onInitialized != null) 
				onInitialized();
		}

		/// <summary>
		/// Sets the uppper and lower bounds for recycling cells.
		/// </summary>
		private void SetRecyclingBounds()
		{
			Viewport.GetWorldCorners(_corners);
			float threshHold = RecyclingThreshold * (_corners[2].y - _corners[0].y);
			_recyclableViewBounds.min = new Vector3(_corners[0].x, _corners[0].y - threshHold);
			_recyclableViewBounds.max = new Vector3(_corners[2].x, _corners[2].y + threshHold);
		}

		/// <summary>
		/// Creates cell Pool for recycling, Caches ICells
		/// </summary>
		private void CreateCellPool()
		{
			//Reseting Pool
			if (_cellPool != null)
			{
				_cellPool.ForEach((RectTransform item) => UnityEngine.Object.Destroy(item.gameObject));
				_cellPool.Clear();
				_cachedCells.Clear();
			}
			else
			{
				_cachedCells = new List<ICell>();
				_cellPool = new List<RectTransform>();
			}

			//Set the prototype cell active and set cell anchor as top 
			PrototypeCell.gameObject.SetActive(true);
			if (IsGrid)
			{
				SetTopLeftAnchor(PrototypeCell);
			}
			else
			{
				SetTopAnchor(PrototypeCell);
			}

			//Reset
			_topMostCellColumn = _bottomMostCellColumn = 0;

			//Temps
			float currentPoolCoverage = 0;
			int poolSize = 0;
			float posX = 0;
			float posY = 0;

			//set new cell size according to its aspect ratio
			_cellWidth = Content.rect.width / _coloumns;
			
			//Get the required pool coverage and mininum size for the Cell pool
			float requriedCoverage = MinPoolCoverage * Viewport.rect.height;
			int minPoolSize = Math.Min(MinPoolSize, DataSource.GetItemCount());

			//create cells untill the Pool area is covered and pool size is the minimum required
			while ((poolSize < minPoolSize || currentPoolCoverage < requriedCoverage) &&
					poolSize < DataSource.GetItemCount())
			{
				float cellHeight = DataSource.GetHeight(poolSize);
				//Instantiate and add to Pool
				RectTransform item = (UnityEngine.Object.Instantiate(PrototypeCell.gameObject))
					.GetComponent<RectTransform>();
				item.name = "Cell";
				item.sizeDelta = new Vector2(_cellWidth, cellHeight);
				_cellPool.Add(item);
				item.SetParent(Content, false);

				if (IsGrid)
				{
					posX = _bottomMostCellColumn * _cellWidth;
					item.anchoredPosition = new Vector2(posX, posY);
					if (++_bottomMostCellColumn >= _coloumns)
					{
						_bottomMostCellColumn = 0;
						posY -= cellHeight;
						currentPoolCoverage += item.rect.height;
					}
				}
				else
				{
					item.anchoredPosition = new Vector2(0, posY);
					posY = item.anchoredPosition.y - item.rect.height;
					currentPoolCoverage += item.rect.height;
				}

				//Setting data for Cell
				_cachedCells.Add(item.GetComponent<ICell>());
				DataSource.SetCell(_cachedCells[_cachedCells.Count - 1], poolSize);
				
				//Update the Pool size
				poolSize++;
			}

			//TODO : you alrady have a _currentColoumn varaiable. Why this calculation?????
			if (IsGrid)
			{
				_bottomMostCellColumn = (_bottomMostCellColumn - 1 + _coloumns) % _coloumns;
			}

			//Deactivate prototype cell if it is not a prefab(i.e. it's present in scene)
			if (PrototypeCell.gameObject.scene.IsValid())
			{
				PrototypeCell.gameObject.SetActive(false);
			}
		}

		#endregion

		#region RECYCLING

		/// <summary>
		/// Recyling entry point
		/// </summary>
		/// <param name="direction">scroll direction </param>
		/// <returns></returns>
		public override Vector2 OnValueChangedListener(Vector2 direction)
		{
			if (_recycling || _cellPool == null || _cellPool.Count == 0) return _zeroVector;

			//Updating Recyclable view bounds since it can change with resolution changes.
			SetRecyclingBounds();

			if (direction.y > 0 && _cellPool[_bottomMostCellIndex].MaxY() > _recyclableViewBounds.min.y)
			{
				return RecycleTopToBottom();
			}
			else if (direction.y < 0 && _cellPool[_topMostCellIndex].MinY() < _recyclableViewBounds.max.y)
			{
				return RecycleBottomToTop();
			}

			return _zeroVector;
		}

		/// <summary>
		/// Recycles cells from top to bottom in the List heirarchy
		/// </summary>
		private Vector2 RecycleTopToBottom()
		{
			_recycling = true;
			float moveOffset = 0f;

			while (_cellPool[_topMostCellIndex].MinY() > _recyclableViewBounds.max.y &&
					_currentItemCount < DataSource.GetItemCount())
			{
				var top = _cellPool[_topMostCellIndex];
				var bottom = _cellPool[_bottomMostCellIndex];

				// amount leaving the top of the window
				float offHeight = top.sizeDelta.y;
				_virtualTop += offHeight;          // << key: virtual scroll advanced
				moveOffset += offHeight;           // compensate to keep visuals stable

				int newDataIndex = _currentItemCount;
				float newHeight = DataSource.GetHeight(newDataIndex);

				// bind first, then size
				DataSource.SetCell(_cachedCells[_topMostCellIndex], newDataIndex);
				top.sizeDelta = new Vector2(top.sizeDelta.x, newHeight);

				// place below the current bottom
				float posY = bottom.anchoredPosition.y - bottom.sizeDelta.y;
				top.anchoredPosition = new Vector2(top.anchoredPosition.x, posY);

				// rotate indices
				_bottomMostCellIndex = _topMostCellIndex;
				_topMostCellIndex = (_topMostCellIndex + 1) % _cellPool.Count;
				_currentItemCount++;
			}

			// window height is ALWAYS sum of pool
			Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, RecalculateWindowHeight());

			if (moveOffset > 0f)
			{
				// keep visuals stable
				_cellPool.ForEach(cell => cell.anchoredPosition += moveOffset * Vector2.up);
				Content.anchoredPosition -= moveOffset * Vector2.up;
			}

			_recycling = false;
			return moveOffset > 0f ? -new Vector2(0, moveOffset) : _zeroVector;
		}

		/// <summary>
		/// Recycles cells from bottom to top in the List heirarchy
		/// </summary>
		private Vector2 RecycleBottomToTop()
		{
			_recycling = true;
			float moveOffset = 0f;

			while (_cellPool[_bottomMostCellIndex].MaxY() < _recyclableViewBounds.min.y &&
					_currentItemCount > _cellPool.Count)
			{
				var bottom = _cellPool[_bottomMostCellIndex];
				var top = _cellPool[_topMostCellIndex];

				_currentItemCount--;
				int newDataIndex = _currentItemCount - _cellPool.Count;
				float newHeight = DataSource.GetHeight(newDataIndex);

				DataSource.SetCell(_cachedCells[_bottomMostCellIndex], newDataIndex);
				bottom.sizeDelta = new Vector2(bottom.sizeDelta.x, newHeight);

				// this much appears at the top of the window
				_virtualTop -= newHeight;          // << key: virtual scroll moved up
				moveOffset += newHeight;

				// place just above current top
				float posY = top.anchoredPosition.y + newHeight;
				bottom.anchoredPosition = new Vector2(bottom.anchoredPosition.x, posY);

				// rotate indices
				_topMostCellIndex = _bottomMostCellIndex;
				_bottomMostCellIndex = (_bottomMostCellIndex - 1 + _cellPool.Count) % _cellPool.Count;
			}

			Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, RecalculateWindowHeight());

			if (moveOffset > 0f)
			{
				_cellPool.ForEach(cell => cell.anchoredPosition -= moveOffset * Vector2.up);
				Content.anchoredPosition += moveOffset * Vector2.up;
			}

			_recycling = false;
			return moveOffset > 0f ? new Vector2(0, moveOffset) : _zeroVector;
		}
		
		private float RecalculateWindowHeight()
		{
			float h = 0f;
			foreach (RectTransform cell in _cellPool)
			{
				h += cell.sizeDelta.y;
			}
			return h;
		}

		#endregion

		#region HELPERS

		/// <summary>
		/// Anchoring cell and content rect transforms to top preset. Makes repositioning easy.
		/// </summary>
		/// <param name="rectTransform"></param>
		private void SetTopAnchor(RectTransform rectTransform)
		{
			//Saving to reapply after anchoring. Width and height changes if anchoring is change. 
			float width = rectTransform.rect.width;
			float height = rectTransform.rect.height;

			//Setting top anchor 
			rectTransform.anchorMin = new Vector2(0.5f, 1);
			rectTransform.anchorMax = new Vector2(0.5f, 1);
			rectTransform.pivot = new Vector2(0.5f, 1);

			//Reapply size
			rectTransform.sizeDelta = new Vector2(width, height);
		}

		private void SetTopLeftAnchor(RectTransform rectTransform)
		{
			//Saving to reapply after anchoring. Width and height changes if anchoring is change. 
			float width = rectTransform.rect.width;
			float height = rectTransform.rect.height;

			//Setting top anchor 
			rectTransform.anchorMin = new Vector2(0, 1);
			rectTransform.anchorMax = new Vector2(0, 1);
			rectTransform.pivot = new Vector2(0, 1);

			//Reapply size
			rectTransform.sizeDelta = new Vector2(width, height);
		}

		#endregion

		#region TESTING

		public void OnDrawGizmos()
		{
			Gizmos.color = Color.green;
			Gizmos.DrawLine(_recyclableViewBounds.min - new Vector3(2000, 0),
				_recyclableViewBounds.min + new Vector3(2000, 0));
			Gizmos.color = Color.red;
			Gizmos.DrawLine(_recyclableViewBounds.max - new Vector3(2000, 0),
				_recyclableViewBounds.max + new Vector3(2000, 0));
		}

		#endregion
	}
}