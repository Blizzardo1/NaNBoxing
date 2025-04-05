using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace NaNBoxing;

internal static unsafe class Program {
    private const ulong MaskSign =          0b1000000000000000000000000000000000000000000000000000000000000000ul;
    private const ulong MaskExponent =      0b0111111111110000000000000000000000000000000000000000000000000000ul;
    private const ulong MaskQuiet =         0b0000000000001000000000000000000000000000000000000000000000000000ul;
    private const ulong MaskType =          0b0000000000000111000000000000000000000000000000000000000000000000ul;
    private const ulong MaskSignature =     0b1111111111111111000000000000000000000000000000000000000000000000ul;
    private const ulong MaskMantissa =      0b0000000000000000111111111111111111111111111111111111111111111111ul;

    // Type IDs for short encoded types
    private const ulong MaskTypeNan =       0b0000000000000000000000000000000000000000000000000000000000000000ul;
    private const ulong MaskTypeFalse =     0b0000000000000001000000000000000000000000000000000000000000000000ul;
    private const ulong MaskTypeTrue =      0b0000000000000010000000000000000000000000000000000000000000000000ul;
    private const ulong MaskTypeNull =      0b0000000000000011000000000000000000000000000000000000000000000000ul;

    // Signatures of encoded types
    private const ulong SignatureNan =      0b0111111111110000000000000000000000000000000000000000000000000000ul;
    private const ulong SignatureFalse =    0b0111111111110001000000000000000000000000000000000000000000000000ul;
    private const ulong SignatureTrue =     0b0111111111110010000000000000000000000000000000000000000000000000ul;
    private const ulong SignatureNull =     0b0111111111110011000000000000000000000000000000000000000000000000ul;
    private const ulong SignatureInt =      0b0111111111110100000000000000000000000000000000000000000000000000ul;
    private const ulong SignatureUnknown1 = 0b0111111111110101000000000000000000000000000000000000000000000000ul;
    private const ulong SignatureUnknown2 = 0b0111111111110110000000000000000000000000000000000000000000000000ul;
    private const ulong SignatureUnknown3 = 0b0111111111110111000000000000000000000000000000000000000000000000ul;



    private enum ValueType {
        TypeFloat,
        TypeBool,
        TypeNull,
        TypeInt,
        TypeArray,
        TypeString,
        TypeObject,
        TypeNone = 255
    }    

    private static T CreateFloat<T>(double value) where T : unmanaged {
        return *(T*)&value;
    }

    private static double DecodeFloat<T>(T value) where T : unmanaged {
        return *(double*)&value;
    }

    private static ValueType GetSignature(ulong value) {
        ulong signature = value & MaskSignature;
        Console.WriteLine($"Inverted: {DBin(~value, true)}");
        if ((~value & MaskExponent) != 0) return ValueType.TypeFloat;

        return signature switch {
            SignatureNan => ValueType.TypeFloat,
            SignatureFalse => ValueType.TypeBool,
            SignatureTrue => ValueType.TypeBool,
            SignatureNull => ValueType.TypeNull,
            SignatureInt => ValueType.TypeInt,
            SignatureUnknown1 => ValueType.TypeArray,
            SignatureUnknown2 => ValueType.TypeString,
            SignatureUnknown3 => ValueType.TypeObject,
            _ => ValueType.TypeNone
        };
    }

    private static string DBin(ulong num, bool spaceOut){
        string s = Convert.ToString((long)num, 2).PadLeft(64, '0');
        if(spaceOut) {
            s = s.Insert(1, " ")
                .Insert(13, " ")
                .Insert(15, " ")
                .Insert(19, " ");
        }

        return s;
    }

    private static void Display(bool b) {
        Console.WriteLine($"Boolean Value {b} exploded is");

        ulong u = CreateFloat< ulong >(b ? 1 : 0)
                  | (ulong)2047 << 52 & MaskExponent
                  | (ulong)( b ? 0b001 : 0b010 ) << 48;
        Console.WriteLine("          S EXPONENT    Q ID  MANTISSA");
        Console.WriteLine($"Bool Val: {DBin(u, true)}");
        ulong sign = u & MaskSign;

        ulong exponents = u & MaskExponent;
        bool quiet = (u & MaskQuiet) == 0;
        ulong type = b ? u & MaskTypeTrue : u & MaskTypeFalse;

        ValueType signature = GetSignature(u);

        ulong mantissa = u & MaskMantissa;
        Console.WriteLine($"Signature: {signature}");
        Console.WriteLine($"Quiet? {(quiet ? "Yes" : "No")}");
        Console.WriteLine($"Sign: {sign}");
        Console.WriteLine($"Type: {Enum.GetName(typeof(ValueType), type)}");
        Console.WriteLine($"Exponents: {exponents}");
        Console.WriteLine($"Mantissa: {mantissa}");
        Console.WriteLine($"Decoded Boolean: {DecodeFloat(u)}\n");
    }

    private static void Display(double num) {
        Console.WriteLine($"Value {num} exploded is");
        //  1 bit  Sign
        // 11 bits Exponents
        //  3 bits ID
        // 48 bits Mantissa
        ulong u = CreateFloat<ulong>(num);
        Console.WriteLine("          S EXPONENT    Q ID  MANTISSA");
        Console.WriteLine($"   Value: {DBin(u, true)}");
        ulong sign = u & MaskSign;

        ulong exponents = u & MaskExponent;
        bool quiet = (u & MaskQuiet) == 0;
        ulong type = u & MaskType;

        ValueType signature = GetSignature(u);

        ulong mantissa = u & MaskMantissa;
        Console.WriteLine($"Signature: {signature}");
        Console.WriteLine($"Quiet? {(quiet ? "Yes" : "No")}");
        Console.WriteLine($"Sign: {sign}");
        Console.WriteLine($"Type: {Enum.GetName(typeof(ValueType), type)}");
        Console.WriteLine($"Exponents: {exponents}");
        Console.WriteLine($"Mantissa: {mantissa}");
        Console.WriteLine($"Decoded: {DecodeFloat(u)}\n");
    }

    private static ulong Pack(byte[] vals) {
        ulong p = 0;
        Console.WriteLine("                                                                                    8765432|8765432|8765432|8765432|8765432|8765432|8765432|8765432|");
        for (int i = 0; i < vals.Length; i++) {
            int bit = 8 * (vals.Length - 1 - i);

            ulong v = (ulong)vals[i] << bit;
            p |= v & MaskMantissa;
            Console.WriteLine($"Pack[{i}]: {vals[ i ],3} << {bit,2} = {Convert.ToString(vals[ i ], 2).PadLeft(8, '0')} = {p:b48} || {v:b64}");
        }

        return p;
    }

    private static byte[] Unpack(ulong val, int length) {
        byte[] b = new byte[length];
        for (int i = 0; i < b.Length; i++) {
            int bit = (8 * (length - 1 - i));
            b[ i ] = (byte)( (byte)( val >> bit ) & 0xFF );
            Console.WriteLine($"Unpack[{i}]: {b[ i ]} >> {bit}");
        }
        return b;
    }
    
    private static void Main(string[] args) {
        Display(100);
        Display(1);
        Display(-1);
        Display(1000);
        Display(-1000);
        Display(16344345123.15632);
        Display(-16344345123.15632);
        Display(16384.768);
        Display(-2048.1024);
        Display(true);
        Display(false);
        Display(double.NaN);
        byte[] vals = "FATMAN"u8.ToArray();
        ulong packed = Pack(vals);
        Console.WriteLine($"Packed Value: {BitConverter.ToString(vals).Replace("-", ", ").Trim(',', ' ')} = {packed:b64}");
        Display(packed);
        byte[] unpacked = Unpack(packed, vals.Length);
        
        if (vals.Where((t, i) => t != unpacked[ i ]).Any()) {
            Console.WriteLine("Verification failed");
            return;
        }

        Console.WriteLine("Verification Passed!");
        Console.WriteLine(Encoding.UTF8.GetString(unpacked));
        
    }
}