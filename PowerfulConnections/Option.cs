using Menu;
using Menu.Remix.MixedUI;
using PowerfulConnections;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ConnectionExtension
{
	internal class Option : OptionInterface
	{
		public Option()
		{
			EnableDebug = config.Bind("EnableDebug", false);
		}

		public override void Initialize()
		{
			base.Initialize();
			List<OpTab> initTab = new List<OpTab>();
			OpTab option = InitNewTab(Custom.rainWorld.inGameTranslator.Translate("Option"));
			initTab.Add(option);
			Tabs = initTab.ToArray();

			const float initYIndex = 1.5f + 1f + 2f;
			float yIndex = initYIndex;

			yIndex = initYIndex;

			//Options
			AppendItems(option, ref yIndex,
				  new OpLabel(Vector2.zero, Vector2.zero, Custom.rainWorld.inGameTranslator.Translate("Enable Debug Log"), FLabelAlignment.Left),
				  new OpCheckBox(EnableDebug, Vector2.zero));
		}

		private void AppendItems(OpTab tab, float overrideSpacing, float maxSizeX, ref float yIndex, params UIelement[] elements)
		{
			for (int i = 0; i < elements.Length; i++)
			{
				var ele = elements[i];
				if (ele == null) continue;
				var size = 1;
				for (int j = i + 1; j < elements.Length; j++)
					if (elements[j] == null) size++;
					else break;
				var sizeX = Mathf.Min(tab.CanvasSize.x / elements.Length * size - 2 * overrideSpacing, maxSizeX);

				ele.pos = new Vector2(tab.CanvasSize.x / elements.Length * i + overrideSpacing +
									  ((tab.CanvasSize.x / elements.Length * size - 2 * overrideSpacing) - sizeX) / 2 +
									  (ele is OpCheckBox ? sizeX - 24 : 0) / 2,
					tab.CanvasSize.y - yIndex * YSize);

				ele.size = new Vector2(sizeX, YItemSize);

				if (ele is UIconfig con)
					con.description = con.cfgEntry.info.description;
			}

			tab.AddItems(elements.Where(i => i != null).ToArray());
			yIndex++;
		}

		private void AppendItems(OpTab tab, float maxSize, ref float yIndex, params UIelement[] elements) =>
			AppendItems(tab, XSpacing, maxSize, ref yIndex, elements);

		private void AppendItems(OpTab tab, ref float yIndex, params UIelement[] elements) =>
			AppendItems(tab, XSpacing, 150, ref yIndex, elements);


		private OpTab InitNewTab(string name, Color? color = null)
		{
			color ??= MenuColorEffect.rgbMediumGrey;
			OpTab tab = new OpTab(this, name) { colorButton = color.Value };
			float yIndex = 1.5f;

			AppendItems(tab, 0, 600, ref yIndex,
				new OpLabel(Vector2.zero, Vector2.zero, Plugin.Name,
					FLabelAlignment.Center, true)
				{ color = color.Value });

			AppendItems(tab, ref yIndex,
				new OpLabel(Vector2.zero, Vector2.zero, $"Version {Plugin.Version}", FLabelAlignment.Left) { color = color.Value },
				new OpLabel(Vector2.zero, Vector2.zero, "by: Pkuyo", FLabelAlignment.Right) { color = color.Value });
			return tab;
		}


		private const float YSize = 35;
		private const float YItemSize = 30;
		private const float XSpacing = 50;

		public readonly Configurable<bool> EnableDebug;
	}
}
