using LeagueToolkit.Core.Meta;
using LeagueToolkit.Core.Meta.Properties;
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
            return prop.Type switch
            {
                BinPropertyType.String => ((BinTreeString)prop).Value,
                BinPropertyType.Hash => hashResolver.ResolveBinHashGeneral(((BinTreeHash)prop).Value),
                BinPropertyType.I8 => ((BinTreeI8)prop).Value,
                BinPropertyType.U8 => ((BinTreeU8)prop).Value,
                BinPropertyType.I16 => ((BinTreeI16)prop).Value,
                BinPropertyType.U16 => ((BinTreeU16)prop).Value,
                BinPropertyType.I32 => ((BinTreeI32)prop).Value,
                BinPropertyType.U32 => ((BinTreeU32)prop).Value,
                BinPropertyType.I64 => ((BinTreeI64)prop).Value,
                BinPropertyType.U64 => ((BinTreeU64)prop).Value,
                BinPropertyType.F32 => ((BinTreeF32)prop).Value,
                BinPropertyType.Bool => ((BinTreeBool)prop).Value,
                BinPropertyType.BitBool => ((BinTreeBitBool)prop).Value,
                BinPropertyType.Vector2 => ((BinTreeVector2)prop).Value,
                BinPropertyType.Vector3 => ((BinTreeVector3)prop).Value,
                BinPropertyType.Vector4 => ((BinTreeVector4)prop).Value,
                BinPropertyType.Matrix44 => ((BinTreeMatrix44)prop).Value,
                BinPropertyType.Color => ((BinTreeColor)prop).Value,
                BinPropertyType.ObjectLink => hashResolver.ResolveBinHashGeneral(((BinTreeObjectLink)prop).Value),
                BinPropertyType.WadChunkLink => hashResolver.ResolveHash(((BinTreeWadChunkLink)prop).Value),
                BinPropertyType.Container => ((BinTreeContainer)prop).Elements.Select(p => ConvertPropertyToValue(p, hashResolver)).ToList(),
                BinPropertyType.UnorderedContainer => ((BinTreeUnorderedContainer)prop).Elements.Select(p => ConvertPropertyToValue(p, hashResolver)).ToList(),
                BinPropertyType.Struct => ((BinTreeStruct)prop).Properties.ToDictionary(kvp => hashResolver.ResolveBinHashGeneral(kvp.Key), kvp => ConvertPropertyToValue(kvp.Value, hashResolver)),
                BinPropertyType.Embedded => ((BinTreeEmbedded)prop).Properties.ToDictionary(kvp => hashResolver.ResolveBinHashGeneral(kvp.Key), kvp => ConvertPropertyToValue(kvp.Value, hashResolver)),
                BinPropertyType.Optional => ConvertPropertyToValue(((BinTreeOptional)prop).Value, hashResolver),
                BinPropertyType.Map => ((BinTreeMap)prop).ToDictionary(kvp => ConvertPropertyToValue(kvp.Key, hashResolver), kvp => ConvertPropertyToValue(kvp.Value, hashResolver)),
                _ => new Dictionary<string, object> { { "Type", prop.Type }, { "NameHash", hashResolver.ResolveBinHashGeneral(prop.NameHash) } }
            };
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
                    kvp => new Dictionary<string, object>
                    {
                        ["Type"] = hashResolver.ResolveBinHashGeneral(kvp.Value.ClassHash),
                        ["Path"] = hashResolver.ResolveBinHashGeneral(kvp.Value.PathHash),
                        ["Properties"] = kvp.Value.Properties.ToDictionary(
                            propKvp => hashResolver.ResolveBinHashGeneral(propKvp.Key),
                            propKvp => ConvertPropertyToValue(propKvp.Value, hashResolver)
                        )
                    }
                )
            };
            return dict;
        }
    }
}
