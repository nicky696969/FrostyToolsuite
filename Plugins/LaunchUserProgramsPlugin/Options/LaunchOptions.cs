using Frosty.Core;
using Frosty.Core.Controls.Editors;
using Frosty.Core.Misc;
using FrostySdk.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaunchUserProgramsPlugin.Options
{

    [IsExpandedByDefault]
    [EbxClassMeta(FrostySdk.IO.EbxFieldType.Struct)]
    public class Programs {
        [EbxFieldMeta(FrostySdk.IO.EbxFieldType.CString)]
        [IsReadOnly]
        public string Name { get; set; }
    }

    [DisplayName("Launch User Programs")]
    public class LaunchOptions : OptionsExtension
    {
        [Category("General")]
        [Description("")]
        [DisplayName("Enabled")]
        [EbxFieldMeta(FrostySdk.IO.EbxFieldType.Boolean)]
        public bool UserProgramLaunchingEnabled { get; set; } = true;

        [Category("General")]
        [Description("")]
        [EbxFieldMeta(FrostySdk.IO.EbxFieldType.Struct)]
        [DisplayName("Programs")]
        [IsReadOnly]
        public List<Programs> UserPrograms { get; set; } = new List<Programs>();

        public override void Load()
        {
            string frostyDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            DirectoryInfo di = new DirectoryInfo($"{frostyDir}\\Plugins\\UserPrograms");
            if (!di.Exists) di.Create();
            foreach (var file in di.GetFiles("*.exe")) {
                UserPrograms.Add(new Programs() { Name = file.Name });
            }

            UserProgramLaunchingEnabled = Config.Get("UserProgramLaunchingEnabled", false, ConfigScope.Game);
        }

        public override void Save()
        {
            Config.Add("UserProgramLaunchingEnabled", UserProgramLaunchingEnabled, ConfigScope.Game);
        }
    }
}
