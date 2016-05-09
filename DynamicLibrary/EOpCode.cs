using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamic
{
    enum EOpCode : byte
    {
        Nop     = 0,
        Break   = 1,
        Ldarg_0 = 2,
        Ldarg_1 = 3,
        Ldarg_2 = 4,
        Ldarg_3 = 5,
        Ldloc_0 = 6,
        Ldloc_1 = 7,
        Ldloc_2 = 8,
        Ldloc_3 = 9,
        Stloc_0 = 0xA,
        Stloc_1 = 0xB,
        Stloc_2 = 0xC,
        Stloc_3 = 0xD,
        Ldarg_S = 0xE,
        Ldarga_S = 0xF,
        Starg_S = 0x10,
        Ldloc_S = 0x11,
        Ldloca_S = 0x12,
        Stloc_S = 0x13,
        Ldnull = 0x14,
        Ldc_I4_M1 = 0x15,
        Ldc_I4_0 = 0x16,
        Ldc_I4_1 = 0x17,
        Ldc_I4_2 = 0x18,
        Ldc_I4_3 = 0x19,
        Ldc_I4_4 = 0x1A,
        Ldc_I4_5 = 0x1B,
        Ldc_I4_6 = 0x1C,
        Ldc_I4_7 = 0x1D,
        Ldc_I4_8 = 0x1E,
        Ldc_I4_S = 0x1F,
        Ldc_I4 = 0x20,
        Ldc_I8 = 0x21,
        Ldc_r4 = 0x22,
        Ldc_r8 = 0x23,
        Dup = 0x25,
        Pop = 0x26,
        Jump = 0x27,
        Call = 0x28,
        Calli = 0x29,
        Ret = 0x2A,
        Br_S = 0x2B,
        Brfalse_S = 0x2C,
        Brtrue_S = 0x2D,
        Beq_S = 0x2E,
        Bge_S = 0x2F,
        Bgt_S = 0x30,
        Ble_S = 0x31,
        Blt_S = 0x32,
        Bne_Un_S = 0x33,
        Bge_Un_S = 0x34,
        Bgt_Un_S = 0x35,
        Ble_Un_S = 0x36,
        Blt_Un_S = 0x37,
        Br = 0x38,
        Brfalse = 0x39,
        Brtrue = 0x3A,
        Beq = 0x3B,
        Bge = 0x3C,
        Bgt = 0x3D,
        Ble = 0x3E,
        Blt = 0x3F,
        Bne_Un = 0x40,
        Bge_Un = 0x41,
        Bgt_Un = 0x42,
        Ble_Un = 0x43,
        Blt_Un = 0x44,
        Switch = 0x45,
        Ldind_I1 = 0x46,
        Ldind_U1 = 0x47,
        Ldind_I2 = 0x48,
        Ldind_U2 = 0x49,
        Ldind_I4 = 0x4A,
        Ldind_U4 = 0x4B,
        Ldind_I8 = 0x4C,
        Ldind_I = 0x4D,
        Ldind_R4 = 0x4E,
        Ldind_R8 = 0x4F,
        Ldind_Ref = 0x50,
        Stind_Ref = 0x51,
        Stind_I1 = 0x52,
        Stind_I2 = 0x53,
        Stind_I4 = 0x54,
        Stind_I8 = 0x55,
        Stind_R4 = 0x56,
        Stind_R8 = 0x57,
        Add = 0x58,
        Sub = 0x59,
        Mul = 0x5A,
        Div = 0x5B,
        Div_Un = 0x5C,
        Rem = 0x5D,
        Rem_Un = 0x5E,
        And = 0x5F,
        Or = 0x60,
        Xor = 0x61,
        Shl = 0x62,
        Shr = 0x63,
        Shr_Un = 0x64,
        Neg = 0x65,
        Not = 0x66,
        Conv_I1 = 0x67,
        Conv_I2 = 0x68,
        Conv_I4 = 0x69,
        Conv_I8 = 0x6A,
        Conv_R4 = 0x6B,
        Conv_R8 = 0x6C,
        Conv_U4 = 0x6D,
        Conv_U8 = 0x6E,
        Callvirt = 0x6F,
        Cpobj = 0x70,
        Ldobj = 0x71,
        Ldstr = 0x72,
        Newobj = 0x73,
        Castclass = 0x74,
        IsInst = 0x75,
        Conv_R_Un = 0x76,
        Unbox = 0x79,
        Throw = 0x7A,
        Ldfld = 0x7B,
        Ldflda = 0x7C,
        Stfld = 0x7D,
        Ldsfld = 0x7E,
        Ldsflda = 0x7F,
        Stsfld = 0x80,
        Stobj = 0x81,
        Conv_Ovf_I1_Un = 0x82,
        Conv_Ovf_I2_Un = 0x83,
        Conv_Ovf_I4_Un = 0x84,
        Conv_Ovf_I8_Un = 0x85,
        Conv_Ovf_U1_Un = 0x86,
        Conv_Ovf_U2_Un = 0x87,
        Conv_Ovf_U4_Un = 0x88,
        Conv_Ovf_U8_Un = 0x89,
        Conv_Ovf_I_Un = 0x8A,
        Conv_Ovf_U_Un = 0x8B,
        Box = 0x8C,
        Newarr = 0x8D,
        Ldlen = 0x8E,
        Ldelema = 0x8F,
        Ldelem_I1 = 0x90,
        Ldelem_U1 = 0x91,
        Ldelem_I2 = 0x92,
        Ldelem_U2 = 0x93,
        Ldelem_I4 = 0x94,
        Ldelem_U4 = 0x95,
        Ldelem_I8 = 0x96,
        Ldelem_I = 0x97,
        Ldelem_R4 = 0x98,
        Ldelem_R8 = 0x99,
        Ldelem_Ref = 0x9A,
        Stelem_I = 0x9B,
        Stelem_I1 = 0x9C,
        Stelem_I2 = 0x9D,
        Stelem_I4 = 0x9E,
        Stelem_I8 = 0x9F,
        Stelem_R4 = 0xA0,
        Stelem_R8 = 0xA1,
        Stelem_Ref = 0xA2,
        Conv_Ovf_I1 = 0xB3,
        Conv_Ovf_U1 = 0xB4,
        Conv_Ovf_I2 = 0xB5,
        Conv_Ovf_U2 = 0xB6,
        Conv_Ovf_I4 = 0xB7,
        Conv_Ovf_U4 = 0xB8,
        Conv_Ovf_I8 = 0xB9,
        Conv_Ovf_U8 = 0xBA,
        RefAnyVal = 0xC2,
        CkFinite = 0xC3,
        MkRefAny = 0xC6,
        LdToken = 0xD0,
        Conv_U2 = 0xD1,
        Conv_U1 = 0xD2,
        Conv_I = 0xD3,
        Conv_Ovf_I = 0xD4,
        Conv_Ovf_U = 0xD5,
        Add_Ovf = 0xD6,
        Add_Ovf_Un = 0xD7,
        Mul_Ovf = 0xD8,
        Mul_Ovf_Un = 0xD9,
        Sub_Ovf = 0xDA,
        Sub_Ovf_Un = 0xDB,
        EndFinally = 0xDC,
        Leave = 0xDD,
        Leave_S = 0xDE,
        Stind_I = 0xDF,
        Conv_U = 0xE0,
        Extended = 0xFE,
        Terminator = 0xFF
    }

    enum EExtendedOpCode
    {
        Arglist = 0,
        Ceq = 1,
        Cgt = 2,
        Cgt_Un = 3,
        Clt = 4,
        Clt_Un = 5,
        LdFtn = 6,
        LdVirtFtn = 7,
        Ldarg = 9,
        Ldarga = 0xA,
        Starg = 0xB,
        Ldloc = 0x0C,
        Ldloca = 0xD,
        Stloc = 0xE,
        Localloc = 0xF,
        EndFilter = 0x11,
        Unaligned = 0x12,
        Volatile = 0x13,
        Tail = 0x14,
        InitObj = 0x15,
        CpBlk = 0x17,
        InitBlk = 0x18,
        Rethrow = 0x1A,
        Sizeof = 0x1C,
        RefAnyType = 0x1D,
    }

    enum EArgumentType
    {
        None,
        Int8,
        Uint8,
        Uint16,
        Int32,
        Uint32,
        Int64,
        Float32,
        Float64,
        Method,
        Type,
        Field,
        String,
        Token,
        ListOfInt,
    }

    static class OpcodeExtensions
    {
        public static EArgumentType ArgFor(this EOpCode op)
        {
            switch (op)
            {
                case EOpCode.Ldc_r4:
                    return EArgumentType.Float32;
                case EOpCode.Ldc_r8:
                    return EArgumentType.Float64;
                case EOpCode.Ldc_I8:
                    return EArgumentType.Int64;
                case EOpCode.Beq:
                case EOpCode.Bge:
                case EOpCode.Bge_Un:
                case EOpCode.Bgt:
                case EOpCode.Bgt_Un:
                case EOpCode.Ble:
                case EOpCode.Ble_Un:
                case EOpCode.Blt:
                case EOpCode.Blt_Un:
                case EOpCode.Bne_Un:
                case EOpCode.Br:
                case EOpCode.Brtrue:
                case EOpCode.Brfalse:
                case EOpCode.Ldc_I4:
                case EOpCode.Leave:
                    return EArgumentType.Int32;
                
                case EOpCode.Beq_S:
                case EOpCode.Bge_S:
                case EOpCode.Bge_Un_S:
                case EOpCode.Bgt_S:
                case EOpCode.Bgt_Un_S:
                case EOpCode.Ble_S:
                case EOpCode.Ble_Un_S:
                case EOpCode.Blt_S:
                case EOpCode.Blt_Un_S:
                case EOpCode.Bne_Un_S:
                case EOpCode.Br_S:
                case EOpCode.Brtrue_S:
                case EOpCode.Brfalse_S:
                case EOpCode.Ldc_I4_S:
                case EOpCode.Leave_S:
                    return EArgumentType.Int8;
                case EOpCode.Ldarg_S:
                case EOpCode.Ldarga_S:
                case EOpCode.Ldloc_S:
                case EOpCode.Ldloca_S:
                case EOpCode.Starg_S:
                case EOpCode.Stloc_S:
                    return EArgumentType.Uint8;
                case EOpCode.Switch:
                    return EArgumentType.ListOfInt;
                case EOpCode.Ldstr:
                    return EArgumentType.String;
                case EOpCode.LdToken:
                    return EArgumentType.Token;
                case EOpCode.Call:
                case EOpCode.Calli:
                case EOpCode.Jump:
                case EOpCode.Callvirt:
                case EOpCode.Newobj:
                    return EArgumentType.Method;
                case EOpCode.Ldfld:
                case EOpCode.Ldflda:
                case EOpCode.Ldsfld:
                case EOpCode.Ldsflda:
                case EOpCode.Stfld:
                case EOpCode.Stsfld:
                    return EArgumentType.Field;
                case EOpCode.Box:
                case EOpCode.Castclass:
                case EOpCode.Cpobj:
                case EOpCode.IsInst:
                case EOpCode.Ldelema:
                case EOpCode.Ldobj:
                case EOpCode.MkRefAny:
                case EOpCode.Newarr:
                case EOpCode.RefAnyVal:
                case EOpCode.Stobj:
                case EOpCode.Unbox:
                    return EArgumentType.Type;
                default:
                    return EArgumentType.None;
            }
        }

        public static EArgumentType ArgFor(this EExtendedOpCode op)
        {
            switch (op)
            {
                case EExtendedOpCode.Unaligned:
                    return EArgumentType.Uint8;
                case EExtendedOpCode.Ldarg:
                case EExtendedOpCode.Ldarga:
                case EExtendedOpCode.Ldloc:
                case EExtendedOpCode.Ldloca:
                case EExtendedOpCode.Starg:
                case EExtendedOpCode.Stloc:
                    return EArgumentType.Uint16;
                case EExtendedOpCode.LdFtn:
                case EExtendedOpCode.LdVirtFtn:
                    return EArgumentType.Method;
                case EExtendedOpCode.InitObj:
                case EExtendedOpCode.Sizeof:
                    return EArgumentType.Type;
                default:
                    return EArgumentType.None;
            }
        }
    }
}
