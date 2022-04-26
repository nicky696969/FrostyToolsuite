using DiceUIVectorShapesEditorPlugin;
using Frosty.Core;
using Frosty.Core.Controls;
using FrostySdk.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DiceUIVectorShapeEditorPlugin
{
    public class DiceUIVectorShapeAssetDefinition : AssetDefinition
    {
        protected static ImageSource imageSource = new ImageSourceConverter().ConvertFromString("pack://application:,,,/SvgImagePlugin;component/Images/SvgFileType.png") as ImageSource;
        public override ImageSource GetIcon()
        {
            return imageSource;
        }

        public override FrostyAssetEditor GetEditor(ILogger logger)
        {
            return new FrostyDiceUIVectorShapeEditor(logger);
        }
    }
}
