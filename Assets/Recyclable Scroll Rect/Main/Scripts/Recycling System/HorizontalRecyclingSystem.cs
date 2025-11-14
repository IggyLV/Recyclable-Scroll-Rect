//MIT License
//Copyright (c) 2020 Mohammed Iqubal Hussain
//Website : Polyandcode.com 

using System;
using System.Collections;
using System.Collections.Generic;
using Recyclable_Scroll_Rect.Main.Scripts.Interfaces;
using UnityEngine;

namespace PolyAndCode.UI
{
    /// <summary>
    /// Recyling system for horizontal type.
    /// </summary>
    public class HorizontalRecyclingSystem : RecyclingSystem
    {
        //Assigned by constructor
        private readonly int _rows;

        //Cell dimensions
        private float _cellWidth, _cellHeight;

        //Pool Generation
        private List<RectTransform> _cellPool;
        private List<ICell> _cachedCells;
        private Bounds _recyclableViewBounds;


        //Temps, Flags
        private readonly Vector3[] _corners = new Vector3[4];
        private bool _recycling;

        //Trackers
        private int currentItemCount; //item count corresponding to the datasource.
        private int leftMostCellIndex, rightMostCellIndex; //Topmost and bottommost cell in the List
        private int _leftMostCellRow, _RightMostCellRow; // used for recyling in Grid layout. leftmost and rightmost row

        //Cached zero vector 
        private Vector2 zeroVector = Vector2.zero;
        
        private float _virtualLeft; //in case we'll need scroll bar
        private readonly float _startOffset;
        private readonly float _endOffset;
        private readonly float _gap;
        
        
        #region INIT
        public HorizontalRecyclingSystem(IRecyclingSystemRequest request)
        {
            PrototypeCell = request.PrototypeCell;
            Viewport = request.Viewport;
            Content = request.Content;
            DataSource = request.DataSource;
            _rows = 1;
            _startOffset = Mathf.Max(0f, request.StartOffset);
            _endOffset = Mathf.Max(0f, request.EndOffset);
            _gap = Mathf.Max(0f, request.Gap);
            _recyclableViewBounds = new Bounds();
        }

        /// <summary>
        /// Corotuine for initiazation.
        /// Using coroutine for init because few UI stuff requires a frame to update
        /// </summary>
        /// <param name="onInitialized">callback when init done</param>
        /// <returns></returns>
        public override IEnumerator InitCoroutine(Action onInitialized)
        {
            //Setting up container and bounds
            SetLeftAnchor(Content);
            Content.anchoredPosition = Vector3.zero;
            yield return null;
            SetRecyclingBounds();

            //Cell Poool
            CreateCellPool();
            
            float windowW = RecalculateWindowWidth();
            Content.sizeDelta = new Vector2(windowW, Content.sizeDelta.y);
            SetLeftAnchor(Content);

// start at the very left of the dataset
            _virtualLeft = 0f;
            
            currentItemCount = _cellPool.Count;
            leftMostCellIndex = 0;
            rightMostCellIndex = _cellPool.Count - 1;

            //Set content width according to no of coloums
            int columns = Mathf.CeilToInt((float)_cellPool.Count / _rows);

            float contentXSize = _startOffset + _endOffset;
            
            for (int i = 0; i < columns; i++)
            {
                contentXSize += DataSource.GetWidth(i);
            }
            
            contentXSize += (columns - 1) * _gap;
            
            Content.sizeDelta = new Vector2(contentXSize, Content.sizeDelta.y);
            SetLeftAnchor(Content);

            if (onInitialized != null) onInitialized();
        }

        /// <summary>
        /// Sets the uppper and lower bounds for recycling cells.
        /// </summary>
        private void SetRecyclingBounds()
        {
            Viewport.GetWorldCorners(_corners);
            float threshHold = RecyclingThreshold * (_corners[2].x - _corners[0].x);
            _recyclableViewBounds.min = new Vector3(_corners[0].x - threshHold, _corners[0].y);
            _recyclableViewBounds.max = new Vector3(_corners[2].x + threshHold, _corners[2].y);
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
            SetLeftAnchor(PrototypeCell);

            //set new cell size according to its aspect ratio
            _cellHeight = Content.rect.height / _rows;
            _cellWidth = PrototypeCell.sizeDelta.x / PrototypeCell.sizeDelta.y * _cellHeight;

            //Reset
            _leftMostCellRow = _RightMostCellRow = 0;

            //Temps
            float currentPoolCoverage = 0;
            int poolSize = 0;
            float posX = _startOffset;
            float posY = 0;

            //Get the required pool coverage and mininum size for the Cell pool
            float requriedCoverage = MinPoolCoverage * Viewport.rect.width;
            int minPoolSize = Math.Min(MinPoolSize, DataSource.GetItemCount());

            //create cells untill the Pool area is covered and pool size is the minimum required
            while ((poolSize < minPoolSize || currentPoolCoverage < requriedCoverage) && poolSize < DataSource.GetItemCount())
            {
                float cellWidth = DataSource.GetWidth(poolSize);
                //Instantiate and add to Pool
                RectTransform item = (UnityEngine.Object.Instantiate(PrototypeCell.gameObject)).GetComponent<RectTransform>();
                item.name = "Cell";
                item.sizeDelta = new Vector2(cellWidth, _cellHeight);
                _cellPool.Add(item);
                item.SetParent(Content, false);

                item.anchoredPosition = new Vector2(posX, 0);
                posX = item.anchoredPosition.x + item.rect.width + _gap;
                currentPoolCoverage += item.rect.width;

                //Setting data for Cell
                _cachedCells.Add(item.GetComponent<ICell>());
                DataSource.SetCell(_cachedCells[_cachedCells.Count - 1], poolSize);

                //Update the Pool size
                poolSize++;
            }

            //Deactivate prototype cell if it is not a prefab(i.e it's present in scene)
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
            if (_recycling || _cellPool == null || _cellPool.Count == 0) return zeroVector;

            //Updating Recyclable view bounds since it can change with resolution changes.
            SetRecyclingBounds();

            if (direction.x < 0 && _cellPool[rightMostCellIndex].MinX() < _recyclableViewBounds.max.x)
            {
                return RecycleLeftToRight();
            }
            else if (direction.x > 0 && _cellPool[leftMostCellIndex].MaxX() > _recyclableViewBounds.min.x)
            {
                return RecycleRightToLeft();
            }
            return zeroVector;
        }

        /// <summary>
        /// Recycles cells from Left to Right in the List heirarchy
        /// </summary>
        private Vector2 RecycleLeftToRight()
        {
            _recycling = true;

            float moveOffset = 0f;

            while (_cellPool[leftMostCellIndex].MaxX() < _recyclableViewBounds.min.x &&
                   currentItemCount < DataSource.GetItemCount())
            {
                // we’re moving the left-most cell to the right end
                RectTransform left = _cellPool[leftMostCellIndex];
                RectTransform right = _cellPool[rightMostCellIndex];

                // how much leaves the window on the left
                float offWidth = left.sizeDelta.x + _gap;
                _virtualLeft += offWidth;     // virtual scroll progressed right
                moveOffset   += offWidth;     // compensate to keep visuals stable

                int newDataIndex = currentItemCount;
                float newWidth = DataSource.GetWidth(newDataIndex);

                // bind first so visuals update; if your width depends on data, update size next
                DataSource.SetCell(_cachedCells[leftMostCellIndex], newDataIndex);
                left.sizeDelta = new Vector2(newWidth, left.sizeDelta.y);

                // if your items can have variable widths, update it here (optional):
                // left.sizeDelta = new Vector2(ComputeWidthFor(newDataIndex), left.sizeDelta.y);

                // place just to the right of current right-most
                float posX = right.anchoredPosition.x + right.sizeDelta.x + _gap;
                left.anchoredPosition = new Vector2(posX, left.anchoredPosition.y);

                // rotate indices
                rightMostCellIndex = leftMostCellIndex;
                leftMostCellIndex  = (leftMostCellIndex + 1) % _cellPool.Count;

                currentItemCount++;
            }

            // window width is ALWAYS the sum of the pool
            Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, RecalculateWindowWidth());

            if (moveOffset > 0f)
            {
                // keep visuals stable relative to viewport
                _cellPool.ForEach(cell => cell.anchoredPosition -= moveOffset * Vector2.right);
                Content.anchoredPosition += moveOffset * Vector2.right;
            }

            _recycling = false;
            return moveOffset > 0f ? new Vector2(moveOffset, 0f) : zeroVector;
        }

        /// <summary>
        /// Recycles cells from Right to Left in the List heirarchy
        /// </summary>
        private Vector2 RecycleRightToLeft()
        {
            _recycling = true;

            float moveOffset = 0f;

            while (_cellPool[rightMostCellIndex].MinX() > _recyclableViewBounds.max.x &&
                   currentItemCount > _cellPool.Count)
            {
                // we’re moving the right-most cell to the left start
                RectTransform right = _cellPool[rightMostCellIndex];
                RectTransform left  = _cellPool[leftMostCellIndex];

                currentItemCount--;
                int newDataIndex = currentItemCount - _cellPool.Count;
                float newWidth = DataSource.GetWidth(newDataIndex);
                
                // bind first
                DataSource.SetCell(_cachedCells[rightMostCellIndex], newDataIndex);
                right.sizeDelta = new Vector2(newWidth, right.sizeDelta.y);

                // if variable widths, set now:
                // right.sizeDelta = new Vector2(ComputeWidthFor(newDataIndex), right.sizeDelta.y);

                // this much appears on the left of the window
                float inWidth = right.sizeDelta.x + _gap;
                _virtualLeft -= inWidth;      // virtual scroll moved left
                moveOffset   += inWidth;

                // place just to the LEFT of current left-most
                float posX = left.anchoredPosition.x - (right.sizeDelta.x + _gap);
                right.anchoredPosition = new Vector2(posX, right.anchoredPosition.y);

                // rotate indices
                leftMostCellIndex  = rightMostCellIndex;
                rightMostCellIndex = (rightMostCellIndex - 1 + _cellPool.Count) % _cellPool.Count;
            }

            // window width = sum of pool
            Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, RecalculateWindowWidth());

            if (moveOffset > 0f)
            {
                _cellPool.ForEach(cell => cell.anchoredPosition += moveOffset * Vector2.right);
                Content.anchoredPosition -= moveOffset * Vector2.right;
            }

            _recycling = false;
            return moveOffset > 0f ? -new Vector2(moveOffset, 0f) : zeroVector;
        }
        
        private float RecalculateWindowWidth()
        {
            if (_cellPool == null || _cellPool.Count == 0)
                return _startOffset + _endOffset;
            
            float width = 0f;
            foreach (RectTransform t in _cellPool)
            {
                width += t.sizeDelta.x;
            }
            
            float gaps = 0f;
            if (_cellPool.Count > 1)
            {
                gaps += (_cellPool.Count - 1) * _gap;
            }

            return width + gaps + _startOffset + _endOffset;
        }
        
        #endregion

        #region  HELPERS
        /// <summary>
        /// Anchoring cell and content rect transforms to top preset. Makes repositioning easy.
        /// </summary>
        /// <param name="rectTransform"></param>
        private void SetLeftAnchor(RectTransform rectTransform)
        {
            //Saving to reapply after anchoring. Width and height changes if anchoring is change. 
            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;

            Vector2 pos = new (0, 0.5f);

            //Setting top anchor 
            rectTransform.anchorMin = pos;
            rectTransform.anchorMax = pos;
            rectTransform.pivot = pos;

            //Reapply size
            rectTransform.sizeDelta = new Vector2(width, height);
        }

        #endregion

        #region  TESTING
        public void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(_recyclableViewBounds.min - new Vector3(0, 2000), _recyclableViewBounds.min + new Vector3(0, 2000));
            Gizmos.color = Color.red;
            Gizmos.DrawLine(_recyclableViewBounds.max - new Vector3(0, 2000), _recyclableViewBounds.max + new Vector3(0, 2000));
        }
        #endregion

    }
}
