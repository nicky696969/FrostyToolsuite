using AtlasTexturePlugin;
using DuplicationPlugin.Windows;
using Frosty.Core;
using Frosty.Core.Viewport;
using Frosty.Core.Windows;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Resources;
using MeshSetPlugin.Resources;
//using SoundEditorPlugin.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DuplicationPlugin
{
    public class TextureExtension : DuplicateAssetExtension
    {
        public override string AssetType => "TextureAsset";

        public override EbxAssetEntry DuplicateAsset(EbxAssetEntry entry, string newName, bool createNew, Type newType)
        {
            // Duplicate the ebx
            EbxAssetEntry newEntry = base.DuplicateAsset(entry, newName, createNew, newType);
            EbxAsset newAsset = App.AssetManager.GetEbx(newEntry);
            dynamic newTextureAsset = newAsset.RootObject;

            // Get the original asset root object data
            EbxAsset asset = App.AssetManager.GetEbx(entry);
            dynamic textureAsset = asset.RootObject;

            // Get the original chunk and res entries
            ResAssetEntry resEntry = App.AssetManager.GetResEntry(textureAsset.Resource);
            Texture texture = App.AssetManager.GetResAs<Texture>(resEntry);
            ChunkAssetEntry chunkEntry = App.AssetManager.GetChunkEntry(texture.ChunkId);

            // Duplicate the res
            ResAssetEntry newResEntry = DuplicateRes(resEntry, newName.ToLower(), ResourceType.Texture);
            Texture newTexture = App.AssetManager.GetResAs<Texture>(newResEntry);
            newTextureAsset.Resource = newResEntry.ResRid;

            // Duplicate the chunk
            Guid chunkGuid = DuplicateChunk(chunkEntry, newTexture.Flags.HasFlag(TextureFlags.OnDemandLoaded) || newTexture.Type != TextureType.TT_2d ? null : newTexture);
            ChunkAssetEntry newChunkEntry = App.AssetManager.GetChunkEntry(chunkGuid);
            newTexture.ChunkId = chunkGuid;

            // Link the newly duplicates ebx, chunk, and res entries together
            newResEntry.LinkAsset(newChunkEntry);
            newEntry.LinkAsset(newResEntry);

            // Modify the newly duplicates ebx, chunk, and res
            App.AssetManager.ModifyEbx(newEntry.Name, newAsset);
            App.AssetManager.ModifyRes(newResEntry.Name, newTexture);

            return newEntry;
        }
    }

    public class AtlasTextureExtension : DuplicateAssetExtension
    {
        public override string AssetType => "AtlasTextureAsset";

        public override EbxAssetEntry DuplicateAsset(EbxAssetEntry entry, string newName, bool createNew, Type newType)
        {
            // Duplicate the ebx
            EbxAssetEntry newEntry = base.DuplicateAsset(entry, newName, createNew, newType);
            EbxAsset newAsset = App.AssetManager.GetEbx(newEntry);
            dynamic newRoot = newAsset.RootObject;

            // Duplicate the res
            ResAssetEntry resEntry = App.AssetManager.GetResEntry(newRoot.Resource);
            ResAssetEntry newResEntry = DuplicateRes(resEntry, newEntry.Name, ResourceType.AtlasTexture);

            // Update the res
            AtlasTexture atlasTexture = App.AssetManager.GetResAs<AtlasTexture>(newResEntry);
            Guid newChunkId = DuplicateChunk(App.AssetManager.GetChunkEntry(atlasTexture.ChunkId));
            atlasTexture.SetData(atlasTexture.Width, atlasTexture.Height, newChunkId, App.AssetManager);


            // Update the ebx
            newRoot.Resource = newResEntry.ResRid;
            newEntry.LinkAsset(resEntry);

            App.AssetManager.ModifyEbx(newEntry.Name, newAsset);
            App.AssetManager.ModifyRes(newResEntry.Name, atlasTexture);

            return newEntry;
        }
    }

    public class MeshExtension : DuplicateAssetExtension
    {
        public override string AssetType => "MeshAsset";

        public override EbxAssetEntry DuplicateAsset(EbxAssetEntry entry, string newName, bool createNew, Type newType)
        {
            // Duplicate the ebx
            EbxAssetEntry newEntry = base.DuplicateAsset(entry, newName, createNew, newType);
            EbxAsset newAsset = App.AssetManager.GetEbx(newEntry);
            dynamic newRoot = newAsset.RootObject;

            // Duplicate the res
            ResAssetEntry oldResEntry = App.AssetManager.GetResEntry(newRoot.MeshSetResource);
            ResAssetEntry newResEntry = DuplicateRes(oldResEntry, newName.ToLower(), ResourceType.MeshSet);

            // Update new meshset
            MeshSet newMeshSet = App.AssetManager.GetResAs<MeshSet>(newResEntry);
            newMeshSet.FullName = newName.ToLower();

            // Duplicate the lod chunks
            foreach (var lod in newMeshSet.Lods)
            {
                ChunkAssetEntry lodChunk = App.AssetManager.GetChunkEntry(lod.ChunkId);
                lod.ChunkId = DuplicateChunk(lodChunk);
                lod.Name = newName.ToLower();
                newResEntry.LinkAsset(App.AssetManager.GetChunkEntry(lod.ChunkId));
            }

            // Update the ebx
            newRoot.MeshSetResource = newResEntry.ResRid;
            newRoot.NameHash = (uint)Utils.HashString(newName.ToLower());
            newEntry.LinkAsset(newResEntry);

            if (ProfilesLibrary.DataVersion == (int)ProfileVersion.StarWarsBattlefrontII)
            {
                // Duplicate the sbd
                ResAssetEntry oldShaderBlock = App.AssetManager.GetResEntry(entry.Name.ToLower() + "_mesh/blocks");
                ResAssetEntry newShaderBlock = DuplicateRes(oldShaderBlock, newName.ToLower() + "_mesh/blocks", ResourceType.ShaderBlockDepot);
                ShaderBlockDepot shaderBlockDepot = App.AssetManager.GetResAs<ShaderBlockDepot>(newShaderBlock);

                // Change the references in the sbd
                for (int lod = 0; lod < newMeshSet.Lods.Count; lod++)
                {
                    ShaderBlockEntry sbEntry = shaderBlockDepot.GetSectionEntry(lod);
                    ShaderBlockMeshVariationEntry sbMvEntry = shaderBlockDepot.GetResource(sbEntry.Index + 1) as ShaderBlockMeshVariationEntry;

                    sbEntry.SetHash(newMeshSet.NameHash, 0, lod);
                    sbMvEntry.SetHash(newMeshSet.NameHash, 0, lod);

                    // Update the mesh guid
                    for (int section = 0; section < newMeshSet.Lods[lod].Sections.Count; section++)
                    {
                        MeshParamDbBlock mesh = sbEntry.GetMeshParams(section);
                        mesh.MeshAssetGuid = newAsset.RootInstanceGuid;
                        mesh.Hash ^= 0xABCDEF;
                    }
                }

                App.AssetManager.ModifyRes(newShaderBlock.Name, shaderBlockDepot);

                newEntry.LinkAsset(newShaderBlock);
            }

            App.AssetManager.ModifyRes(newResEntry.Name, newMeshSet);
            App.AssetManager.ModifyEbx(newEntry.Name, newAsset);

            return newEntry;
        }
    }

    public class ObjectVariationExtension : DuplicateAssetExtension
    {
        public override string AssetType => "ObjectVariation";

        public override EbxAssetEntry DuplicateAsset(EbxAssetEntry entry, string newName, bool createNew, Type newType)
        {
            if (ProfilesLibrary.DataVersion != (int)ProfileVersion.StarWarsBattlefrontII)
                return base.DuplicateAsset(entry, newName, createNew, newType);

            // Duplicate the ebx
            EbxAssetEntry newEntry = base.DuplicateAsset(entry, newName, createNew, newType);
            EbxAsset newAsset = App.AssetManager.GetEbx(newEntry);
            dynamic newRoot = newAsset.RootObject;

            // Get namehash needed for the sbd
            uint nameHash = newRoot.NameHash;

            foreach (var mv in MeshVariationDb.FindVariations(nameHash))
            {
                // Get meshSet
                EbxAssetEntry meshEntry = App.AssetManager.GetEbxEntry(mv.MeshGuid);
                EbxAsset meshAsset = App.AssetManager.GetEbx(meshEntry);
                dynamic meshRoot = meshAsset.RootObject;
                ResAssetEntry meshRes = App.AssetManager.GetResEntry(meshRoot.MeshSetResource);
                MeshSet meshSet = App.AssetManager.GetResAs<MeshSet>(meshRes);

                // Dupe sbd
                ResAssetEntry resEntry = App.AssetManager.GetResEntry(entry.Name.ToLower() + "/" + meshEntry.Filename + "_" + (uint)Utils.HashString(meshEntry.Name, true) + "/shaderblocks_variation/blocks");
                ResAssetEntry newResEntry = DuplicateRes(resEntry, newName.ToLower() + "/" + meshEntry.Filename + "_" + (uint)Utils.HashString(meshEntry.Name, true) + "/shaderblocks_variation/blocks", ResourceType.ShaderBlockDepot);
                ShaderBlockDepot newShaderBlockDepot = App.AssetManager.GetResAs<ShaderBlockDepot>(newResEntry);

                // change namehash so the sbd hash can be calculated corretcly
                nameHash = (uint)Utils.HashString(newName, true);
                newRoot.NameHash = nameHash;

                // Change the references in the sbd
                for (int lod = 0; lod < meshSet.Lods.Count; lod++)
                {
                    ShaderBlockEntry sbEntry = newShaderBlockDepot.GetSectionEntry(lod);
                    ShaderBlockMeshVariationEntry sbMvEntry = newShaderBlockDepot.GetResource(sbEntry.Index + 1) as ShaderBlockMeshVariationEntry;

                    sbEntry.SetHash(meshSet.NameHash, nameHash, lod);
                    sbMvEntry.SetHash(meshSet.NameHash, nameHash, lod);
                }

                App.AssetManager.ModifyRes(newResEntry.Name, newShaderBlockDepot);
                newEntry.LinkAsset(newResEntry);
                App.AssetManager.ModifyEbx(newName, newAsset);

                break;
            }

            return newEntry;
        }
    }

    public class SvgImageExtension : DuplicateAssetExtension
    {
        public override string AssetType => "SvgImage";

        public override EbxAssetEntry DuplicateAsset(EbxAssetEntry entry, string newName, bool createNew, Type newType)
        {
            // Duplicate the ebx
            EbxAssetEntry newEntry = base.DuplicateAsset(entry, newName, createNew, newType);
            EbxAsset newAsset = App.AssetManager.GetEbx(newEntry);
            dynamic newRoot = newAsset.RootObject;

            // Duplicate the res
            ResAssetEntry resEntry = App.AssetManager.GetResEntry(newRoot.Resource);
            ResAssetEntry newResEntry = DuplicateRes(resEntry, newEntry.Name, ResourceType.SvgImage);

            // Update the ebx
            newRoot.Resource = newResEntry.ResRid;
            newEntry.LinkAsset(resEntry);

            App.AssetManager.ModifyEbx(newEntry.Name, newAsset);

            return newEntry;
        }
    }

    public class SoundWaveExtension : DuplicateAssetExtension
    {
        public override string AssetType => "SoundWaveAsset";

        public override EbxAssetEntry DuplicateAsset(EbxAssetEntry entry, string newName, bool createNew, Type newType)
        {
            // Duplicate the ebx
            EbxAssetEntry newEntry = base.DuplicateAsset(entry, newName, createNew, newType);
            EbxAsset newAsset = App.AssetManager.GetEbx(newEntry);
            dynamic newRoot = newAsset.RootObject;

            // Duplicate the chunks
            foreach (dynamic chunk in newRoot.Chunks)
            {
                ChunkAssetEntry soundChunk = App.AssetManager.GetChunkEntry(chunk.ChunkId);
                Guid chunkId = DuplicateChunk(soundChunk);

                chunk.ChunkId = chunkId;
                newEntry.LinkAsset(App.AssetManager.GetChunkEntry(chunkId));
            }

            App.AssetManager.ModifyEbx(newEntry.Name, newAsset);

            return newEntry;
        }
    }

    public class OctaneAssetExtension : DuplicateAssetExtension
    {
        public override string AssetType => "OctaneAsset";

        public override EbxAssetEntry DuplicateAsset(EbxAssetEntry entry, string newName, bool createNew, Type newType)
        {
            // Duplicate the ebx
            EbxAssetEntry newEntry = base.DuplicateAsset(entry, newName, createNew, newType);
            EbxAsset newAsset = App.AssetManager.GetEbx(newEntry);
            dynamic newRoot = newAsset.RootObject;

            // Duplicate the chunks
            foreach (dynamic chunk in newRoot.Chunks)
            {
                ChunkAssetEntry soundChunk = App.AssetManager.GetChunkEntry(chunk.ChunkId);
                Guid chunkId = DuplicateChunk(soundChunk);

                chunk.ChunkId = chunkId;
                newEntry.LinkAsset(App.AssetManager.GetChunkEntry(chunkId));
            }

            App.AssetManager.ModifyEbx(newEntry.Name, newAsset);

            return newEntry;
        }
    }

    //public class NewWaveExtension : DuplicateAssetExtension
    //{
    //    public override string AssetType => "NewWaveAsset";

    //    public override EbxAssetEntry DuplicateAsset(EbxAssetEntry entry, string newName, bool createNew, Type newType)
    //    {
    //        // Duplicate the ebx
    //        EbxAssetEntry newEntry = base.DuplicateAsset(entry, newName, createNew, newType);
    //        EbxAsset newAsset = App.AssetManager.GetEbx(newEntry);
    //        dynamic newRoot = newAsset.RootObject;

    //        //Duplicate res
    //        ResAssetEntry resEntry = App.AssetManager.GetResEntry(entry.Name.ToLower());
    //        ResAssetEntry newRes = DuplicateRes(resEntry, newName.ToLower(), ResourceType.NewWaveResource);
    //        NewWaveResource newWave = App.AssetManager.GetResAs<NewWaveResource>(newRes);

    //        // Duplicate the chunks
    //        for (int i = 0; i < newRoot.Chunks.Count; i++)
    //        {
    //            ChunkAssetEntry soundChunk = App.AssetManager.GetChunkEntry(newRoot.Chunks[i].ChunkId);
    //            Guid chunkId = DuplicateChunk(soundChunk);

    //            newRoot.Chunks[i].ChunkId = chunkId;
    //            newWave.Chunks[i].ChunkId = chunkId;
    //        }

    //        App.AssetManager.ModifyEbx(newEntry.Name, newAsset);

    //        return newEntry;
    //    }
    //}

    public class DuplicateAssetExtension
    {
        public virtual string AssetType => null;

        public virtual EbxAssetEntry DuplicateAsset(EbxAssetEntry entry, string newName, bool createNew, Type newType)
        {
            EbxAsset asset = App.AssetManager.GetEbx(entry);
            EbxAsset newAsset = null;

            if (createNew)
            {
                newAsset = new EbxAsset(TypeLibrary.CreateObject(newType.Name));
            }
            else
            {
                using (EbxBaseWriter writer = EbxBaseWriter.CreateWriter(new MemoryStream(), EbxWriteFlags.DoNotSort | EbxWriteFlags.IncludeTransient))
                {
                    writer.WriteAsset(asset);
                    byte[] buf = writer.ToByteArray();
                    using (EbxReader reader = EbxReader.CreateReader(new MemoryStream(buf)))
                        newAsset = reader.ReadAsset<EbxAsset>();
                }
            }

            newAsset.SetFileGuid(Guid.NewGuid());

            dynamic obj = newAsset.RootObject;
            obj.Name = newName;

            AssetClassGuid guid = new AssetClassGuid(Utils.GenerateDeterministicGuid(newAsset.Objects, (Type)obj.GetType(), newAsset.FileGuid), -1);
            obj.SetInstanceGuid(guid);

            EbxAssetEntry newEntry = App.AssetManager.AddEbx(newName, newAsset);

            newEntry.AddedBundles.AddRange(entry.EnumerateBundles());
            newEntry.ModifiedEntry.DependentAssets.AddRange(newAsset.Dependencies);

            return newEntry;
        }

        public static Guid DuplicateChunk(ChunkAssetEntry entry, Texture texture = null)
        {
            byte[] random = new byte[16];
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            while (true)
            {
                rng.GetBytes(random);
                if (App.AssetManager.GetChunkEntry(new Guid(random)) == null)
                    break;
                else
                    App.Logger.Log("Randomised onto old guid: " + random.ToString());
            }
            Guid newGuid = App.AssetManager.AddChunk(((MemoryStream)App.AssetManager.GetChunk(entry)).ToArray(), new Guid(random), texture, entry.EnumerateBundles().ToArray());
            App.Logger.Log(string.Format("Duped chunk {0} to {1}", entry.Name, newGuid));
            return newGuid;
        }
        public static ResAssetEntry DuplicateRes(ResAssetEntry entry, string Name, ResourceType resType)
        {
            if (App.AssetManager.GetResEntry(Name) == null)
            {
                ResAssetEntry newEntry = App.AssetManager.AddRes(Name, resType, entry.ResMeta, ((MemoryStream)App.AssetManager.GetRes(entry)).ToArray(), entry.EnumerateBundles().ToArray());
                App.Logger.Log(string.Format("Duped res {0} to {1}", entry.Name, newEntry.Name));
                return newEntry;
            }
            else
            {
                App.Logger.Log(Name + " already has a res files");
                return null;
            }
        }
    }

    public class DuplicateContextMenuItem : DataExplorerContextMenuExtension
    {
        private Dictionary<string, DuplicateAssetExtension> extensions = new Dictionary<string, DuplicateAssetExtension>();

        public DuplicateContextMenuItem()
        {
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsSubclassOf(typeof(DuplicateAssetExtension)))
                {
                    var extension = (DuplicateAssetExtension)Activator.CreateInstance(type);
                    extensions.Add(extension.AssetType, extension);
                }
            }
            extensions.Add("null", new DuplicateAssetExtension());
        }

        public override string ContextItemName => "Duplicate";

        public override RelayCommand ContextItemClicked => new RelayCommand((o) =>
        {
            EbxAssetEntry entry = App.SelectedAsset as EbxAssetEntry;
            EbxAsset asset = App.AssetManager.GetEbx(entry);

            DuplicateAssetWindow win = new DuplicateAssetWindow(entry);
            if (win.ShowDialog() == false)
                return;

            string newName = win.SelectedPath + "/" + win.SelectedName;
            newName = newName.Trim('/');

            Type newType = win.SelectedType;
            FrostyTaskWindow.Show("Duplicating asset", "", (task) =>
            {
                if (!MeshVariationDb.IsLoaded)
                    MeshVariationDb.LoadVariations(task);

                try
                {
                    string key = "null";
                    foreach (string typekey in extensions.Keys)
                    {
                        if (TypeLibrary.IsSubClassOf(entry.Type, typekey))
                        {
                            key = typekey;
                            break;
                        }
                    }

                    extensions[key].DuplicateAsset(entry, newName, newType != null, newType);
                }
                catch (Exception e)
                {
                    App.Logger.Log($"Failed to duplicate {entry.Name}");
                }
            });

            App.EditorWindow.DataExplorer.RefreshAll();
        });
    }
}
