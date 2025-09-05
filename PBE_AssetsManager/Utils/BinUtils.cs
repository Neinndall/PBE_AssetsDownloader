using LeagueToolkit.Core.Meta;
using LeagueToolkit.Core.Meta.Properties;
using PBE_AssetsManager.Services.Hashes;
using PBE_AssetsManager.Services;
using System.Collections.Generic;
using System.Linq;

namespace PBE_AssetsManager.Utils
{
    public static class BinUtils
    {
        public static object ConvertPropertyToValue(BinTreeProperty prop, HashResolverService hashResolver)
        {
            if (prop == null) return null;

            switch (prop.Type)
            {
                case BinPropertyType.String: return ((BinTreeString)prop).Value;
                case BinPropertyType.Hash: return hashResolver.ResolveBinHashGeneral(((BinTreeHash)prop).Value);
                case BinPropertyType.I8: return ((BinTreeI8)prop).Value;
                case BinPropertyType.U8: return ((BinTreeU8)prop).Value;
                case BinPropertyType.I16: return ((BinTreeI16)prop).Value;
                case BinPropertyType.U16: return ((BinTreeU16)prop).Value;
                case BinPropertyType.I32: return ((BinTreeI32)prop).Value;
                case BinPropertyType.U32: return ((BinTreeU32)prop).Value;
                case BinPropertyType.I64: return ((BinTreeI64)prop).Value;
                case BinPropertyType.U64: return ((BinTreeU64)prop).Value;
                case BinPropertyType.F32: return ((BinTreeF32)prop).Value;
                case BinPropertyType.Bool: return ((BinTreeBool)prop).Value;
                case BinPropertyType.BitBool: return ((BinTreeBitBool)prop).Value;
                case BinPropertyType.Vector2: return ((BinTreeVector2)prop).Value;
                case BinPropertyType.Vector3: return ((BinTreeVector3)prop).Value;
                case BinPropertyType.Vector4: return ((BinTreeVector4)prop).Value;
                case BinPropertyType.Matrix44: return ((BinTreeMatrix44)prop).Value;
                case BinPropertyType.Color: return ((BinTreeColor)prop).Value;
                case BinPropertyType.ObjectLink: return hashResolver.ResolveBinHashGeneral(((BinTreeObjectLink)prop).Value);
                case BinPropertyType.WadChunkLink: return hashResolver.ResolveHash(((BinTreeWadChunkLink)prop).Value);
                case BinPropertyType.Container: return ((BinTreeContainer)prop).Elements.Select(p => ConvertPropertyToValue(p, hashResolver)).ToList();
                case BinPropertyType.UnorderedContainer: return ((BinTreeUnorderedContainer)prop).Elements.Select(p => ConvertPropertyToValue(p, hashResolver)).ToList();
                case BinPropertyType.Struct:
                    {
                        var structProp = (BinTreeStruct)prop;
                        var dict = structProp.Properties.ToDictionary(kvp => hashResolver.ResolveBinHashGeneral(kvp.Key), kvp => ConvertPropertyToValue(kvp.Value, hashResolver));
                        dict["type"] = hashResolver.ResolveBinHashGeneral(structProp.ClassHash);
                        return dict;
                    }
                case BinPropertyType.Embedded:
                    {
                        var embeddedProp = (BinTreeEmbedded)prop;
                        var dict = embeddedProp.Properties.ToDictionary(kvp => hashResolver.ResolveBinHashGeneral(kvp.Key), kvp => ConvertPropertyToValue(kvp.Value, hashResolver));
                        dict["type"] = hashResolver.ResolveBinHashGeneral(embeddedProp.ClassHash);
                        return dict;
                    }
                case BinPropertyType.Optional: return ConvertPropertyToValue(((BinTreeOptional)prop).Value, hashResolver);
                case BinPropertyType.Map: return ((BinTreeMap)prop).ToDictionary(kvp => ConvertPropertyToValue(kvp.Key, hashResolver), kvp => ConvertPropertyToValue(kvp.Value, hashResolver));
                default:
                    return new Dictionary<string, object> { { "Type", prop.Type }, { "NameHash", hashResolver.ResolveBinHashGeneral(prop.NameHash) } };
            }
        }

        public static Dictionary<string, object> ConvertBinTreeToDictionary(BinTree binTree, HashResolverService hashResolver)
        {
            var dict = new Dictionary<string, object>
            {
                ["IsOverride"] = binTree.IsOverride,
                ["Dependencies"] = binTree.Dependencies.Select(depHashString => {
                    if (uint.TryParse(depHashString, System.Globalization.NumberStyles.HexNumber, null, out uint depHashUint))
                    {
                        return hashResolver.ResolveBinHashGeneral(depHashUint);
                    }
                    return depHashString;
                }).ToList(),
                ["Objects"] = binTree.Objects.ToDictionary(
                    kvp => hashResolver.ResolveBinHashGeneral(kvp.Key),
                    kvp => {
                        var objDict = kvp.Value.Properties.ToDictionary(
                            propKvp => hashResolver.ResolveBinHashGeneral(propKvp.Key),
                            propKvp => ConvertPropertyToValue(propKvp.Value, hashResolver)
                        );
                        objDict["type"] = hashResolver.ResolveBinHashGeneral(kvp.Value.ClassHash);
                        return objDict;
                    }
                )
            };
            return dict;
        }
    }
}