using PolyAndCode.UI;
using UnityEngine;

namespace Recyclable_Scroll_Rect.Main.Scripts.Interfaces
{
	public interface IRecyclingSystemRequest
	{
		public RectTransform PrototypeCell { get; }
		public RectTransform Viewport { get; }
		public RectTransform Content{ get; }
		public IRecyclableScrollRectDataSource DataSource{ get; }
		public float StartOffset { get; }
		public float EndOffset { get; }
		public float Gap { get; }
	}
}
