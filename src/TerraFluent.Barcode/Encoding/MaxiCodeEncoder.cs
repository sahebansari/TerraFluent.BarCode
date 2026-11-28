// Portions adapted from ZXing (https://github.com/zxing/zxing), Copyright 2011 ZXing authors,
// licensed under the Apache License, Version 2.0; modified (tables inverted for encoding).
// See THIRD-PARTY-NOTICES.md.

namespace TerraFluent.Barcode.Encoding;

/// <summary>
/// MaxiCode (ISO/IEC 16023): a fixed-size symbol of 884 hexagonal modules in
/// 33 rows of 30 around a bullseye finder, used on parcels. Mode 4 carries
/// 93 data codewords with standard error correction, mode 5 77 codewords with
/// enhanced error correction, mode 6 reader programming. Modes 2 and 3 start
/// with a structured carrier message (postal code, country, service class).
/// </summary>
/// <remarks>
/// The module placement table and the character sets come from ISO/IEC 16023
/// as reproduced in ZXing (Apache License 2.0); see THIRD-PARTY-NOTICES.md.
/// </remarks>
internal static class MaxiCodeEncoder
{
    public const int Columns = 30;
    public const int Rows = 33;

    /// <summary>ISO/IEC 16023 asks for a quiet zone of one module on every side.</summary>
    public const int QuietZone = 1;

    private const int Pad = 33;
    private const int NumericShift = 31;
    private const int LatchA = 63, LatchB = 63, ShiftA = 59, ShiftB = 59, ShiftC = 60, ShiftD = 61, ShiftE = 62;

    // Special entries of the character set tables.
    private const char Special = '￿';

    // Sets A to E (64 entries each); Special marks shift, latch, pad, ECI and numeric-shift codes.
    private static readonly string[] Sets =
    {
        "\rABCDEFGHIJKLMNOPQRSTUVWXYZ￿\u001C\u001D\u001E￿ ￿\"#$%&'()*+,-./0123456789:￿￿￿￿￿",
        "`abcdefghijklmnopqrstuvwxyz￿\u001C\u001D\u001E￿{￿}~\u007F;<=>?[\\]^_ ,./:@!|￿￿￿￿￿￿￿￿￿",
        "ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖ×ØÙÚ" +
            "￿\u001C\u001D\u001E￿" +
            "ÛÜÝÞßª¬±²³µ¹º¼½¾\u0080\u0081\u0082\u0083\u0084\u0085\u0086\u0087\u0088\u0089" +
            "￿ ￿￿￿￿",
        "àáâãäåæçèéêëìíîïðñòóôõö÷øùú" +
            "￿\u001C\u001D\u001E￿" +
            "ûüýþÿ¡¨«¯°´·¸»¿\u008A\u008B\u008C\u008D\u008E\u008F\u0090\u0091\u0092\u0093\u0094" +
            "￿ ￿￿￿￿",
        "\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008\u0009\n\u000B\u000C\r\u000E\u000F\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001A" +
            "￿￿￿\u001B￿\u001C\u001D\u001E" +
            "\u001F\u009F ¢£¤¥¦§©­®¶\u0095\u0096\u0097\u0098\u0099\u009A\u009B\u009C\u009D\u009E" +
            "￿ ￿￿￿￿",
    };

    // For each Latin-1 character, (set << 8) | code in the preferred set, or -1. Sets A and B are preferred
    // because characters there need no shift.
    private static readonly int[] Lookup = BuildLookup();

    // Bit number (0-863, most significant bit of codeword 0 first) of each module, row by row;
    // -2 = dark orientation module, -1 = light orientation module, -3 = unused or bullseye (light).
    private static readonly short[] BitNumbers =
    {
        121, 120, 127, 126, 133, 132, 139, 138, 145, 144, 151, 150, 157, 156, 163, 162, 169, 168, 175, 174, 181, 180, 187, 186, 193, 192, 199, 198, -2, -2, // row 0
        123, 122, 129, 128, 135, 134, 141, 140, 147, 146, 153, 152, 159, 158, 165, 164, 171, 170, 177, 176, 183, 182, 189, 188, 195, 194, 201, 200, 816, -3, // row 1
        125, 124, 131, 130, 137, 136, 143, 142, 149, 148, 155, 154, 161, 160, 167, 166, 173, 172, 179, 178, 185, 184, 191, 190, 197, 196, 203, 202, 818, 817, // row 2
        283, 282, 277, 276, 271, 270, 265, 264, 259, 258, 253, 252, 247, 246, 241, 240, 235, 234, 229, 228, 223, 222, 217, 216, 211, 210, 205, 204, 819, -3, // row 3
        285, 284, 279, 278, 273, 272, 267, 266, 261, 260, 255, 254, 249, 248, 243, 242, 237, 236, 231, 230, 225, 224, 219, 218, 213, 212, 207, 206, 821, 820, // row 4
        287, 286, 281, 280, 275, 274, 269, 268, 263, 262, 257, 256, 251, 250, 245, 244, 239, 238, 233, 232, 227, 226, 221, 220, 215, 214, 209, 208, 822, -3, // row 5
        289, 288, 295, 294, 301, 300, 307, 306, 313, 312, 319, 318, 325, 324, 331, 330, 337, 336, 343, 342, 349, 348, 355, 354, 361, 360, 367, 366, 824, 823, // row 6
        291, 290, 297, 296, 303, 302, 309, 308, 315, 314, 321, 320, 327, 326, 333, 332, 339, 338, 345, 344, 351, 350, 357, 356, 363, 362, 369, 368, 825, -3, // row 7
        293, 292, 299, 298, 305, 304, 311, 310, 317, 316, 323, 322, 329, 328, 335, 334, 341, 340, 347, 346, 353, 352, 359, 358, 365, 364, 371, 370, 827, 826, // row 8
        409, 408, 403, 402, 397, 396, 391, 390, 79, 78, -2, -2, 13, 12, 37, 36, 2, -1, 44, 43, 109, 108, 385, 384, 379, 378, 373, 372, 828, -3, // row 9
        411, 410, 405, 404, 399, 398, 393, 392, 81, 80, 40, -2, 15, 14, 39, 38, 3, -1, -1, 45, 111, 110, 387, 386, 381, 380, 375, 374, 830, 829, // row 10
        413, 412, 407, 406, 401, 400, 395, 394, 83, 82, 41, -3, -3, -3, -3, -3, 5, 4, 47, 46, 113, 112, 389, 388, 383, 382, 377, 376, 831, -3, // row 11
        415, 414, 421, 420, 427, 426, 103, 102, 55, 54, 16, -3, -3, -3, -3, -3, -3, -3, 20, 19, 85, 84, 433, 432, 439, 438, 445, 444, 833, 832, // row 12
        417, 416, 423, 422, 429, 428, 105, 104, 57, 56, -3, -3, -3, -3, -3, -3, -3, -3, 22, 21, 87, 86, 435, 434, 441, 440, 447, 446, 834, -3, // row 13
        419, 418, 425, 424, 431, 430, 107, 106, 59, 58, -3, -3, -3, -3, -3, -3, -3, -3, -3, 23, 89, 88, 437, 436, 443, 442, 449, 448, 836, 835, // row 14
        481, 480, 475, 474, 469, 468, 48, -2, 30, -3, -3, -3, -3, -3, -3, -3, -3, -3, -3, 0, 53, 52, 463, 462, 457, 456, 451, 450, 837, -3, // row 15
        483, 482, 477, 476, 471, 470, 49, -1, -2, -3, -3, -3, -3, -3, -3, -3, -3, -3, -3, -3, -2, -1, 465, 464, 459, 458, 453, 452, 839, 838, // row 16
        485, 484, 479, 478, 473, 472, 51, 50, 31, -3, -3, -3, -3, -3, -3, -3, -3, -3, -3, 1, -2, 42, 467, 466, 461, 460, 455, 454, 840, -3, // row 17
        487, 486, 493, 492, 499, 498, 97, 96, 61, 60, -3, -3, -3, -3, -3, -3, -3, -3, -3, 26, 91, 90, 505, 504, 511, 510, 517, 516, 842, 841, // row 18
        489, 488, 495, 494, 501, 500, 99, 98, 63, 62, -3, -3, -3, -3, -3, -3, -3, -3, 28, 27, 93, 92, 507, 506, 513, 512, 519, 518, 843, -3, // row 19
        491, 490, 497, 496, 503, 502, 101, 100, 65, 64, 17, -3, -3, -3, -3, -3, -3, -3, 18, 29, 95, 94, 509, 508, 515, 514, 521, 520, 845, 844, // row 20
        559, 558, 553, 552, 547, 546, 541, 540, 73, 72, 32, -3, -3, -3, -3, -3, -3, 10, 67, 66, 115, 114, 535, 534, 529, 528, 523, 522, 846, -3, // row 21
        561, 560, 555, 554, 549, 548, 543, 542, 75, 74, -2, -1, 7, 6, 35, 34, 11, -2, 69, 68, 117, 116, 537, 536, 531, 530, 525, 524, 848, 847, // row 22
        563, 562, 557, 556, 551, 550, 545, 544, 77, 76, -2, 33, 9, 8, 25, 24, -1, -2, 71, 70, 119, 118, 539, 538, 533, 532, 527, 526, 849, -3, // row 23
        565, 564, 571, 570, 577, 576, 583, 582, 589, 588, 595, 594, 601, 600, 607, 606, 613, 612, 619, 618, 625, 624, 631, 630, 637, 636, 643, 642, 851, 850, // row 24
        567, 566, 573, 572, 579, 578, 585, 584, 591, 590, 597, 596, 603, 602, 609, 608, 615, 614, 621, 620, 627, 626, 633, 632, 639, 638, 645, 644, 852, -3, // row 25
        569, 568, 575, 574, 581, 580, 587, 586, 593, 592, 599, 598, 605, 604, 611, 610, 617, 616, 623, 622, 629, 628, 635, 634, 641, 640, 647, 646, 854, 853, // row 26
        727, 726, 721, 720, 715, 714, 709, 708, 703, 702, 697, 696, 691, 690, 685, 684, 679, 678, 673, 672, 667, 666, 661, 660, 655, 654, 649, 648, 855, -3, // row 27
        729, 728, 723, 722, 717, 716, 711, 710, 705, 704, 699, 698, 693, 692, 687, 686, 681, 680, 675, 674, 669, 668, 663, 662, 657, 656, 651, 650, 857, 856, // row 28
        731, 730, 725, 724, 719, 718, 713, 712, 707, 706, 701, 700, 695, 694, 689, 688, 683, 682, 677, 676, 671, 670, 665, 664, 659, 658, 653, 652, 858, -3, // row 29
        733, 732, 739, 738, 745, 744, 751, 750, 757, 756, 763, 762, 769, 768, 775, 774, 781, 780, 787, 786, 793, 792, 799, 798, 805, 804, 811, 810, 860, 859, // row 30
        735, 734, 741, 740, 747, 746, 753, 752, 759, 758, 765, 764, 771, 770, 777, 776, 783, 782, 789, 788, 795, 794, 801, 800, 807, 806, 813, 812, 861, -3, // row 31
        737, 736, 743, 742, 749, 748, 755, 754, 761, 760, 767, 766, 773, 772, 779, 778, 785, 784, 791, 790, 797, 796, 803, 802, 809, 808, 815, 814, 863, 862, // row 32
    };

    private static int[] BuildLookup()
    {
        var lookup = new int[256];
        for (var i = 0; i < lookup.Length; i++) lookup[i] = -1;
        for (var set = Sets.Length - 1; set >= 0; set--)
        {
            if (Sets[set].Length != 64) throw new InvalidOperationException("MaxiCode character set " + set + " is malformed.");
            for (var code = 0; code < 64; code++)
            {
                var c = Sets[set][code];
                if (c != Special) lookup[c] = (set << 8) | code;
            }
        }

        return lookup;
    }

    /// <summary>Encodes <paramref name="value"/> in mode 4, 5 or 6.</summary>
    public static BarcodeMatrix Encode(string value, int mode)
    {
        var capacity = mode == 5 ? 77 : 93;
        var data = Message(value, capacity, nameof(value));
        var codewords = new int[144];
        codewords[0] = mode;
        Array.Copy(data, 0, codewords, 1, 9);
        Array.Copy(data, 9, codewords, 20, capacity - 9);
        return Build(codewords, enhanced: mode == 5);
    }

    /// <summary>
    /// Mode 2 (numeric postal code of up to 9 digits) or mode 3 (alphanumeric
    /// postal code of up to 6 characters) with a structured carrier message.
    /// </summary>
    public static BarcodeMatrix EncodeStructured(string postalCode, int countryCode, int serviceClass, string message)
    {
        if (postalCode is null) throw new ArgumentNullException(nameof(postalCode));
        if (message is null) throw new ArgumentNullException(nameof(message));
        Compatibility.Guard.InRange(countryCode, 0, 999, nameof(countryCode));
        Compatibility.Guard.InRange(serviceClass, 0, 999, nameof(serviceClass));
        if (postalCode.Length == 0) throw new ArgumentException("The postal code must not be empty.", nameof(postalCode));

        var codewords = new int[144];
        var numeric = postalCode.Length <= 9 && postalCode.All(c => c is >= '0' and <= '9');
        if (numeric)
        {
            var postcode = int.Parse(postalCode, System.Globalization.CultureInfo.InvariantCulture);
            var length = postalCode.Length;
            codewords[0] = ((postcode & 0x03) << 4) | 2;
            codewords[1] = (postcode & 0xFC) >> 2;
            codewords[2] = (postcode & 0x3F00) >> 8;
            codewords[3] = (postcode & 0xFC000) >> 14;
            codewords[4] = (postcode & 0x3F00000) >> 20;
            codewords[5] = ((postcode & 0x3C000000) >> 26) | ((length & 0x3) << 4);
            codewords[6] = (length & 0x3C) >> 2;
        }
        else
        {
            if (postalCode.Length > 6)
            {
                throw new ArgumentException(
                    $"A MaxiCode postal code is up to 9 digits or up to 6 letters, digits and spaces; got {postalCode.Length} characters.",
                    nameof(postalCode));
            }

            var pc = new int[6];
            for (var i = 0; i < 6; i++)
            {
                var c = i < postalCode.Length ? postalCode[i] : ' ';
                var code = c < 256 ? Lookup[c] : -1;
                if (code < 0 || code >> 8 != 0)
                {
                    throw new ArgumentException(
                        $"Character '{c}' at index {i} cannot be used in an alphanumeric MaxiCode postal code. Allowed: A-Z, 0-9, space and \" # $ % & ' ( ) * + , - . / :.",
                        nameof(postalCode));
                }

                pc[i] = code & 0x3F;
            }

            codewords[0] = ((pc[5] & 0x03) << 4) | 3;
            codewords[1] = ((pc[4] & 0x03) << 4) | ((pc[5] & 0x3C) >> 2);
            codewords[2] = ((pc[3] & 0x03) << 4) | ((pc[4] & 0x3C) >> 2);
            codewords[3] = ((pc[2] & 0x03) << 4) | ((pc[3] & 0x3C) >> 2);
            codewords[4] = ((pc[1] & 0x03) << 4) | ((pc[2] & 0x3C) >> 2);
            codewords[5] = ((pc[0] & 0x03) << 4) | ((pc[1] & 0x3C) >> 2);
            codewords[6] = (pc[0] & 0x3C) >> 2;
        }

        codewords[6] |= (countryCode & 0x3) << 4;
        codewords[7] = (countryCode & 0xFC) >> 2;
        codewords[8] = ((countryCode & 0x300) >> 8) | ((serviceClass & 0xF) << 2);
        codewords[9] = (serviceClass & 0x3F0) >> 4;

        var data = Message(message, 84, nameof(message));
        Array.Copy(data, 0, codewords, 20, 84);
        return Build(codewords, enhanced: false);
    }

    /// <summary>
    /// Text to codewords: Sets A and B by latch or shift, Sets C, D and E by
    /// shift, runs of 9 digits by numeric shift, then padding.
    /// </summary>
    internal static int[] Message(string value, int capacity, string paramName)
    {
        var output = new List<int>(capacity);
        var set = 0;
        var i = 0;
        while (i < value.Length)
        {
            if (DigitRun(value, i) >= 9)
            {
                var n = int.Parse(value.Substring(i, 9), System.Globalization.CultureInfo.InvariantCulture);
                output.Add(NumericShift);
                output.Add((n >> 24) & 0x3F);
                output.Add((n >> 18) & 0x3F);
                output.Add((n >> 12) & 0x3F);
                output.Add((n >> 6) & 0x3F);
                output.Add(n & 0x3F);
                i += 9;
                continue;
            }

            var c = value[i];
            var entry = c < 256 ? Lookup[c] : -1;
            if (entry < 0)
            {
                throw new ArgumentException(
                    $"Character '{c}' at index {i} cannot be encoded in MaxiCode, which supports ISO-8859-1 (Latin-1) text only.",
                    paramName);
            }

            var target = entry >> 8;
            var code = entry & 0x3F;
            if (IndexIn(set, c) is var here && here >= 0)
            {
                output.Add(here);
            }
            else if (target <= 1)
            {
                // Latch for a run of three or more, or a run that reaches the end; shift otherwise.
                var run = 0;
                while (i + run < value.Length && IndexIn(set, value[i + run]) < 0 && IndexIn(target, value[i + run]) >= 0) run++;
                if (run >= 3 || i + run == value.Length)
                {
                    output.Add(target == 0 ? LatchA : LatchB);
                    set = target;
                }
                else
                {
                    output.Add(target == 0 ? ShiftA : ShiftB);
                }

                output.Add(code);
            }
            else
            {
                output.Add(target switch { 2 => ShiftC, 3 => ShiftD, _ => ShiftE });
                output.Add(code);
            }

            i++;
        }

        if (output.Count > capacity)
        {
            throw new ArgumentException(
                $"The value needs {output.Count} MaxiCode codewords but this mode holds {capacity}. Shorten it" +
                (capacity == 77 ? " or use mode 4." : "."),
                paramName);
        }

        while (output.Count < capacity) output.Add(Pad);
        return output.ToArray();
    }

    private static int IndexIn(int set, char c) => c == Special ? -1 : Sets[set].IndexOf(c);

    private static int DigitRun(string value, int start)
    {
        var i = start;
        while (i < value.Length && value[i] is >= '0' and <= '9') i++;
        return i - start;
    }

    private static BarcodeMatrix Build(int[] codewords, bool enhanced)
    {
        var field = GaloisField.MaxiCode;
        field.Encode(codewords, 0, 10, 10);
        var (data, ecc) = enhanced ? (34, 28) : (42, 20);
        field.Encode(codewords, 20, data, ecc, stride: 2);
        field.Encode(codewords, 21, data, ecc, stride: 2);

        var modules = new bool[Columns * Rows];
        for (var i = 0; i < modules.Length; i++)
        {
            int bit = BitNumbers[i];
            modules[i] = bit == -2 || (bit >= 0 && ((codewords[bit / 6] >> (5 - bit % 6)) & 1) == 1);
        }

        return new BarcodeMatrix(Columns, Rows, modules, isLinear: false, QuietZone) { IsHexagonal = true };
    }
}
