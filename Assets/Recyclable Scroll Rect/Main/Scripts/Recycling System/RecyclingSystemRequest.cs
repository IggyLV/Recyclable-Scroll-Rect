using PolyAndCode.UI;
using Recyclable_Scroll_Rect.Main.Scripts.Interfaces;
using UnityEngine;

namespace Recyclable_Scroll_Rect.Main.Scripts.Recycling_System
{
	public class RecyclingSystemRequest : IRecyclingSystemRequest
	{
		public RectTransform PrototypeCell { get; }
		public RectTransform Viewport { get; }
		public RectTransform Content { get; }
		public IRecyclableScrollRectDataSource DataSource { get; }
		public float StartOffset { get; }
		public float EndOffset { get; }
		public float Gap { get; }

		public RecyclingSystemRequest(
			RectTransform prototypeCell,
			RectTransform viewport,
			RectTransform content,
			IRecyclableScrollRectDataSource dataSource,
			float startOffset,
			float endOffset,
			float gap
		)
		{
			PrototypeCell = prototypeCell;
			Viewport = viewport;
			Content = content;
			DataSource = dataSource;
			StartOffset = startOffset;
			EndOffset = endOffset;
			Gap = gap;
		}
	}
}
