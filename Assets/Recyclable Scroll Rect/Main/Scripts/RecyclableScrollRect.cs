//MIT License
//Copyright (c) 2020 Mohammed Iqubal Hussain
//Website : Polyandcode.com 

using System;
using Recyclable_Scroll_Rect.Main.Scripts.Recycling_System;
using UnityEngine;
using UnityEngine.UI;

namespace PolyAndCode.UI
{
	/// <summary>
	/// Entry for the recycling system. Extends Unity's inbuilt ScrollRect.
	/// </summary>
	public class RecyclableScrollRect : ScrollRect
	{
		[HideInInspector]
		public IRecyclableScrollRectDataSource DataSource;

		[SerializeField]
		private float startOffset = 0f;

		[SerializeField]
		private float endOffset = 0f;

		[SerializeField]
		private float gap = 0f;

		//Prototype cell can either be a prefab or present as a child to the content(will automatically be disabled in runtime)
		[SerializeField]
		private RectTransform prototypeCell;

		//If true the intiziation happens at Start. Controller must assign the datasource in Awake.
		//Set to false if self init is not required and use public init API.
		[SerializeField]
		private bool selfInitialize = true;

		public enum DirectionType
		{
			Vertical,
			Horizontal
		}

		[SerializeField]
		private DirectionType direction;

		//Segments : coloums for vertical and rows for horizontal.
		public int Segments
		{
			set { segments = Math.Max(value, 2); }
			get { return segments; }
		}

		[SerializeField]
		private int segments;

		private RecyclingSystem _recyclingSystem;
		private Vector2 _prevAnchoredPos;

		protected override void Start()
		{
			//defafult(built-in) in scroll rect can have both directions enabled, Recyclable scroll rect can be scrolled in only one direction.
			//setting default as vertical, Initialize() will set this again. 
			vertical = true;
			horizontal = false;

			if (!Application.isPlaying) return;

			if (selfInitialize) Initialize();
		}

		/// <summary>
		/// Initialization when selfInitalize is true. Assumes that data source is set in controller's Awake.
		/// </summary>
		private void Initialize()
		{
			RecyclingSystemRequest request = new (
				prototypeCell,
				viewport,
				content,
				DataSource,
				startOffset,
				endOffset,
				gap
			);

			//Contruct the recycling system.
			if (direction == DirectionType.Vertical)
			{
				_recyclingSystem = new VerticalRecyclingSystem(request);
			}
			else if (direction == DirectionType.Horizontal)
			{
				_recyclingSystem = new HorizontalRecyclingSystem(request);
			}

			vertical = direction == DirectionType.Vertical;
			horizontal = direction == DirectionType.Horizontal;

			_prevAnchoredPos = content.anchoredPosition;
			onValueChanged.RemoveListener(OnValueChangedListener);
			//Adding listener after pool creation to avoid any unwanted recycling behaviour.(rare scenerio)
			StartCoroutine(_recyclingSystem.InitCoroutine(() =>
				onValueChanged.AddListener(OnValueChangedListener)
			));
		}

		/// <summary>
		/// public API for Initializing when datasource is not set in controller's Awake. Make sure selfInitalize is set to false. 
		/// </summary>
		public void Initialize(IRecyclableScrollRectDataSource dataSource)
		{
			DataSource = dataSource;
			Initialize();
		}

		/// <summary>
		/// Added as a listener to the OnValueChanged event of Scroll rect.
		/// Recycling entry point for recyling systems.
		/// </summary>
		/// <param name="direction">scroll direction</param>
		public void OnValueChangedListener(Vector2 normalizedPos)
		{
			Vector2 dir = content.anchoredPosition - _prevAnchoredPos;
			m_ContentStartPosition += _recyclingSystem.OnValueChangedListener(dir);
			_prevAnchoredPos = content.anchoredPosition;
		}

		/// <summary>
		///Reloads the data. Call this if a new datasource is assigned.
		/// </summary>
		public void ReloadData()
		{
			ReloadData(DataSource);
		}

		/// <summary>
		/// Overloaded ReloadData with dataSource param
		///Reloads the data. Call this if a new datasource is assigned.
		/// </summary>
		public void ReloadData(IRecyclableScrollRectDataSource dataSource)
		{
			if (_recyclingSystem != null)
			{
				StopMovement();
				onValueChanged.RemoveListener(OnValueChangedListener);
				_recyclingSystem.DataSource = dataSource;
				StartCoroutine(_recyclingSystem.InitCoroutine(() =>
					onValueChanged.AddListener(OnValueChangedListener)
				));
				_prevAnchoredPos = content.anchoredPosition;
			}
		}

		/*
		#region Testing
		private void OnDrawGizmos()
		{
			if (_recyclableScrollRect is VerticalRecyclingSystem)
			{
				((VerticalRecyclingSystem)_recyclableScrollRect).OnDrawGizmos();
			}

			if (_recyclableScrollRect is HorizontalRecyclingSystem)
			{
				((HorizontalRecyclingSystem)_recyclableScrollRect).OnDrawGizmos();
			}

		}
		#endregion
		*/
	}
}
