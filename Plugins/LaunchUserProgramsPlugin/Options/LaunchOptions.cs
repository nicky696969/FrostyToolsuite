using Frosty.Core;
using Frosty.Core.Controls.Editors;
using Frosty.Core.Misc;
using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaunchUserProgramsPlugin.Options
{

    public class FrostyPlatformDataEditor : FrostyCustomComboDataEditor<string, string> {
    }



    [IsExpandedByDefault]
    [EbxClassMeta(EbxFieldType.Struct)]
    public class Program {
        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.CString)]
        public CString Name { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        [Editor(typeof(FrostyPlatformDataEditor))]
        public CustomComboData<string, string> Pack { get; set; }

        [IsReadOnly]
        [IsHidden]
        [EbxFieldMeta(EbxFieldType.CString)]
        public CString NameFull { get; set; }
    }

    [DisplayName("Launch User Programs")]
    public class LaunchOptions : OptionsExtension
    {
        [Category("General")]
        [Description("")]
        [DisplayName("Enabled")]
        [EbxFieldMeta(EbxFieldType.Boolean)]
        public bool UserProgramLaunchingEnabled { get; set; } = true;

        [Category("General")]
        [Description("")]
        [DisplayName("Programs")]
        [IsReadOnly]
        [IsExpandedByDefault]
        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<Program> UserPrograms { get; set; } = new List<Program>();

        public override void Load()
        {
            string frostyDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            DirectoryInfo di = new DirectoryInfo($"{frostyDir}\\Plugins\\UserPrograms\\");
            if (!di.Exists) di.Create();
            foreach (var file in di.GetFiles("*.exe")) {
                List<string> packs = Config.EnumerateKeys(ConfigScope.Pack).ToList();
                packs.Insert(0, "All Profiles");
                if (file.Name.Contains("{{")) {
                    string fileName = file.Name.Substring(file.Name.IndexOf("}}") + 2);
                    string pack = file.Name.Substring(2, file.Name.IndexOf("}}") - 2);
                    UserPrograms.Add(new Program() { Name = fileName, Pack = new CustomComboData<string, string>(packs, packs) { SelectedIndex = packs.IndexOf(pack) }, NameFull = file.Name });
                }
                else {
                    UserPrograms.Add(new Program() { Name = file.Name, Pack = new CustomComboData<string, string>(packs, packs), NameFull = file.Name });
                }
            }

            UserProgramLaunchingEnabled = Config.Get("UserProgramLaunchingEnabled", false, ConfigScope.Game);

            
        }

        public override void Save()
        {
            Config.Add("UserProgramLaunchingEnabled", UserProgramLaunchingEnabled, ConfigScope.Game);

            string frostyDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            DirectoryInfo di = new DirectoryInfo($"{frostyDir}\\Plugins\\UserPrograms\\");

            foreach (var program in UserPrograms) {
                if (program.Pack.SelectedName != "All Profiles") {
                    File.Move(di.FullName + program.NameFull.ToString(), di.FullName + "{{" + program.Pack.SelectedName + "}}" + program.Name.ToString());
                }
                else if (program.NameFull.ToString().Contains("{{")) {
                    string newFileName = program.NameFull.ToString().Substring(program.NameFull.ToString().IndexOf("}}") + 2);
                    File.Move(di.FullName + program.NameFull.ToString(), di.FullName + newFileName);
                }
            }
        }
    }
}
