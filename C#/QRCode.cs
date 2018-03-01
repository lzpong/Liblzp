using System;
using System.Collections.Generic;
using System.Drawing;

//---------------------------------------------------------------------
// QRCode for JavaScript
//
// Copyright (c) 2009 Kazuhiko Arase
//
// URL: http://www.d-project.com/
//
// Licensed under the MIT license:
//   http://www.opensource.org/licenses/mit-license.php
//
// The word "QR Code" is registered trademark of 
// DENSO WAVE INCORPORATED
//   http://www.denso-wave.com/qrcode/faqpatent-e.html
//
// Transfer to C# by lzpong 2018/2/24
// The Code Transfered from https://github.com/jeromeetienne/jquery-qrcode/blob/master/src/qrcode.js
//---------------------------------------------------------------------
//二维码制作
namespace Lzp.QRCode
{
    //---------------------------------------------------------------------
    /// <summary>
    /// 模式
    /// </summary>
    public enum QRMode
    {
        MODE_NUMBER =		1 << 0,
        MODE_ALPHA_NUM = 	1 << 1,
        MODE_8BIT_BYTE = 	1 << 2,
        MODE_KANJI =		1 << 3
    }
    //---------------------------------------------------------------------
    /// <summary>
    /// 容错率
    /// </summary>
    public enum QRErrorCorrectLevel
    {
        /// <summary>
        /// L = ~7% correction
        /// </summary>
        L = 1,
        /// <summary>
        /// M = ~15% correction
        /// </summary>
        M = 0,
        /// <summary>
        /// Q = ~25% correction
        /// </summary>
        Q = 3,
        /// <summary>
        /// H = ~30% correction
        /// </summary>
        H = 2
    }
    //---------------------------------------------------------------------
    /// <summary>
    /// 掩码模式
    /// </summary>
    enum QRMaskPattern
    {
        PATTERN000 = 0,
        PATTERN001 = 1,
        PATTERN010 = 2,
        PATTERN011 = 3,
        PATTERN100 = 4,
        PATTERN101 = 5,
        PATTERN110 = 6,
        PATTERN111 = 7
    }

    //---------------------------------------------------------------------
    /// <summary>
    /// 8位字节
    /// </summary>
    class QR8bitByte
    {
        public QRMode mode;
        public string data;
        public QR8bitByte(string data) {
            mode = QRMode.MODE_8BIT_BYTE;
            this.data = data;
        }
        internal int getLength()
        {
            return data.Length;
        }
        internal void write(QRBitBuffer buffer)
        {
            for (var i = 0; i < this.data.Length; i++)
            {
                // not JIS ...
                buffer.put(data[i], 8);
            }
        }
    }

    //---------------------------------------------------------------------
    /// <summary>
    /// 二维码
    /// </summary>
    public class QRCode
    {
        int typeNumber;
        int moduleCount;
        List<QR8bitByte> dataList;
        List<byte> dataCache;
        QRErrorCorrectLevel errorCorrectLevel;
        Dictionary<int, Dictionary<int, bool?>> modules;
        static int PAD0 = 0xEC;
        static int PAD1 = 0x11;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="typeNumber">模式</param>
        /// <param name="errorCorrectLevel">容错率</param>
        public QRCode(QRMode typeNumber, QRErrorCorrectLevel errorCorrectLevel)
        {
            this.typeNumber = (int)typeNumber;
            this.errorCorrectLevel = errorCorrectLevel;
            this.modules = null;
            this.moduleCount = 0;
            this.dataCache = null;
            this.dataList = new List<QR8bitByte>();
        }
        /// <summary>
        /// 添加数据
        /// </summary>
        /// <param name="data">数据</param>
        public void addData(string data)
        {
            var newData = new QR8bitByte(data);
            this.dataList.Add(newData);
            this.dataCache = null;
        }
        /// <summary>
        /// 获取二维码图片
        /// </summary>
        /// <param name="size">图片尺寸</param>
        /// <param name="front">前景色</param>
        /// <param name="back">背景色</param>
        public Bitmap getBitmap(int size, Color front = new Color() /*Color.Black*/, Color back= new Color() /*Color.White*/)
        {
            if (front.IsEmpty)
                front = Color.Black;
            if (back.IsEmpty)
                back = Color.White;
            int resolution = size / this.moduleCount;
            if (resolution < 1)
                resolution = 1;
            size = resolution * this.moduleCount;
            Bitmap img = new Bitmap(size, size);
            for (int row = 0; row < this.moduleCount; row++) //行
            {
                for (int col = 0; col < this.moduleCount; col++)//列
                {
                    Color pixelColor = modules[row][col]==true? front:back;
                    //区块着色
                    for(int i=row* resolution; i< (row+1)*resolution;i++)
                    {
                        for(int j=col* resolution; j< (col+1)*resolution;j++)
                        {
                            img.SetPixel(i, j, pixelColor);
                        }
                    }

                }
            }
            return img;
        }
        /// <summary>
        /// 获取二维码字符图
        /// </summary>
        public string getModulesStr(char front= '█',char back= '□')
        {
            string s = "";
            for(var i=0;i<moduleCount;i++)
            {
                for (var j = 0; j < moduleCount; j++)
                    s += modules[i][j] == null ? '〇': (modules[i][j]==true ? front : back);
                s += "\r\n";
            }
            return s;
        }

        /// <summary>
        /// 判断二维码上的一个点是否是前景色
        /// </summary>
        /// <param name="row">第几行</param>
        /// <param name="col">第几列</param>
        public bool? isDark(int row, int col)
        {
            if (row < 0 || this.moduleCount <= row || col < 0 || this.moduleCount <= col)
            {
                throw new Exception("行列越界" + row + "," + col);
            }
            return this.modules[row][col];
        }
        /// <summary>
        /// 返回二维码大小(宽度或高度,二维码 高度==宽度)
        /// </summary>
        public int getModuleCount()
        {
            return this.moduleCount;
        }
        /// <summary>
        /// 生成二维码
        /// </summary>
        public void make()
        {
            // Calculate automatically typeNumber if provided is < 1
            if (this.typeNumber < 1)
            {
                var typeNumber = 1;
                for (typeNumber = 1; typeNumber < 40; typeNumber++)
                {
                    var rsBlocks = QRRSBlock.getRSBlocks(typeNumber, this.errorCorrectLevel);

                    var buffer = new QRBitBuffer();
                    var totalDataCount = 0;
                    for (var i = 0; i < rsBlocks.Count; i++)
                    {
                        totalDataCount += rsBlocks[i].dataCount;
                    }

                    for (var i = 0; i < this.dataList.Count; i++)
                    {
                        var data = this.dataList[i];
                        buffer.put((int)data.mode, 4);
                        buffer.put(data.getLength(), QRUtil.getLengthInBits(data.mode, typeNumber));
                        data.write(buffer);
                    }
                    if (buffer.getLengthInBits() <= totalDataCount * 8)
                        break;
                }
                this.typeNumber = typeNumber;
            }
            this.makeImpl(false, getBestMaskPattern());
        }

        void makeImpl(bool test, QRMaskPattern maskPattern)
        {

            this.moduleCount = this.typeNumber * 4 + 17;
            this.modules = new Dictionary<int, Dictionary<int, bool?>>(this.moduleCount);

            for (var row = 0; row < this.moduleCount; row++)
            {

                this.modules[row] = new Dictionary<int, bool?>(this.moduleCount);

                for (var col = 0; col < this.moduleCount; col++)
                {
                    this.modules[row][col] = null;//(col + row) % 3;
                }
            }

            this.setupPositionProbePattern(0, 0);
            this.setupPositionProbePattern(this.moduleCount - 7, 0);
            this.setupPositionProbePattern(0, this.moduleCount - 7);
            this.setupPositionAdjustPattern();
            this.setupTimingPattern();
            this.setupTypeInfo(test, maskPattern);

            if (this.typeNumber >= 7)
            {
                this.setupTypeNumber(test);
            }

            if (this.dataCache == null)
            {
                this.dataCache = createData(this.typeNumber, this.errorCorrectLevel, this.dataList);
            }

            this.mapData(this.dataCache, maskPattern);
        }

        void setupPositionProbePattern(int row, int col)
        {

            for (var r = -1; r <= 7; r++)
            {

                if (row + r <= -1 || this.moduleCount <= row + r) continue;

                for (var c = -1; c <= 7; c++)
                {

                    if (col + c <= -1 || this.moduleCount <= col + c) continue;

                    if ((0 <= r && r <= 6 && (c == 0 || c == 6))
                            || (0 <= c && c <= 6 && (r == 0 || r == 6))
                            || (2 <= r && r <= 4 && 2 <= c && c <= 4))
                    {
                        this.modules[row + r][col + c] = true;
                    }
                    else
                    {
                        this.modules[row + r][col + c] = false;
                    }
                }
            }
        }

        QRMaskPattern getBestMaskPattern()
        {

            var minLostPoint = 0;
            QRMaskPattern pattern = 0;

            for (var i = 0; i < 8; i++)
            {

                this.makeImpl(true, (QRMaskPattern)i);

                var lostPoint = QRUtil.getLostPoint(this);

                if (i == 0 || minLostPoint > lostPoint)
                {
                    minLostPoint = lostPoint;
                    pattern = (QRMaskPattern)i;
                }
            }

            return pattern;
        }

        void setupTimingPattern()
        {

            for (var r = 8; r < this.moduleCount - 8; r++)
            {
                if (this.modules[r][6] != null)
                {
                    continue;
                }
                this.modules[r][6] = (r % 2 == 0);
            }

            for (var c = 8; c < this.moduleCount - 8; c++)
            {
                if (this.modules[6][c] != null)
                {
                    continue;
                }
                this.modules[6][c] = (c % 2 == 0);
            }
        }

        void setupPositionAdjustPattern()
        {
            var pos = QRUtil.getPatternPosition(this.typeNumber);

            for (var i = 0; i < pos.Length; i++)
            {
                for (var j = 0; j < pos.Length; j++)
                {
                    var row = pos[i];
                    var col = pos[j];

                    if (modules[row][col] != null)
                    {
                        continue;
                    }

                    for (var r = -2; r <= 2; r++)
                    {
                        for (var c = -2; c <= 2; c++)
                        {
                            if (r == -2 || r == 2 || c == -2 || c == 2
                                || (r == 0 && c == 0))
                            {
                                modules[row + r][col + c] = true;
                            }
                            else
                            {
                                modules[row + r][col + c] = false;
                            }
                        }
                    }
                }
            }
        }

        void setupTypeNumber(bool test)
        {

            int bits = QRUtil.getBCHTypeNumber(this.typeNumber);

            for (int i = 0; i < 18; i++)
            {
                var mod = (!test && ((bits >> i) & 1) == 1);
                this.modules[(int)Math.Floor((i / 3.0))][i % 3 + this.moduleCount - 8 - 3] = mod;
            }

            for (int i = 0; i < 18; i++)
            {
                var mod = (!test && ((bits >> i) & 1) == 1);
                this.modules[i % 3 + this.moduleCount - 8 - 3][(int)Math.Floor(i / 3.0)] = mod;
            }
        }

        void setupTypeInfo(bool test, QRMaskPattern maskPattern)
        {

            int data = ((int)this.errorCorrectLevel << 3) | (int)maskPattern;
            int bits = QRUtil.getBCHTypeInfo(data);

            // vertical		
            for (var i = 0; i < 15; i++)
            {
                bool mod = !test && ((bits >> i) & 1) == 1;

                if (i < 6)
                {
                    this.modules[i][8] = mod;
                }
                else if (i < 8)
                {
                    this.modules[i + 1][8] = mod;
                }
                else
                {
                    this.modules[this.moduleCount - 15 + i][8] = mod;
                }
            }

            // horizontal
            for (var i = 0; i < 15; i++)
            {

                var mod = !test && ((bits >> i) & 1) == 1;

                if (i < 8)
                {
                    this.modules[8][this.moduleCount - i - 1] = mod;
                }
                else if (i < 9)
                {
                    this.modules[8][15 - i - 1 + 1] = mod;
                }
                else
                {
                    this.modules[8][15 - i - 1] = mod;
                }
            }

            // fixed module
            this.modules[this.moduleCount - 8][8] = !test;

        }

        void mapData(List<byte> data, QRMaskPattern maskPattern)
        {

            var inc = -1;
            var row = this.moduleCount - 1;
            var bitIndex = 7;
            var byteIndex = 0;

            for (var col = this.moduleCount - 1; col > 0; col -= 2)
            {
                if (col == 6) col--;

                while (true)
                {
                    for (var c = 0; c < 2; c++)
                    {
                        if (this.modules[row][col - c] == null)
                        {
                            var dark = false;

                            if (byteIndex < data.Count)
                            {
                                dark = (((data[byteIndex] >> bitIndex) & 1) == 1);
                            }

                            var mask = QRUtil.getMask(maskPattern, row, col - c);

                            if (mask)
                            {
                                dark = !dark;
                            }

                            this.modules[row][col - c] = dark;
                            bitIndex--;

                            if (bitIndex == -1)
                            {
                                byteIndex++;
                                bitIndex = 7;
                            }
                        }
                    }

                    row += inc;

                    if (row < 0 || this.moduleCount <= row)
                    {
                        row -= inc;
                        inc = -inc;
                        break;
                    }
                }
            }

        }

        List<byte> createData(int typeNumber, QRErrorCorrectLevel errorCorrectLevel, List<QR8bitByte> dataList)
        {

            var rsBlocks = QRRSBlock.getRSBlocks(typeNumber, errorCorrectLevel);

            var buffer = new QRBitBuffer();

            for (var i = 0; i < dataList.Count; i++)
            {
                var data = dataList[i];
                buffer.put((int)data.mode, 4);
                buffer.put(data.getLength(), QRUtil.getLengthInBits(data.mode, typeNumber));
                data.write(buffer);
            }

            // calc num max data.
            var totalDataCount = 0;
            for (var i = 0; i < rsBlocks.Count; i++)
            {
                totalDataCount += rsBlocks[i].dataCount;
            }

            if (buffer.getLengthInBits() > totalDataCount * 8)
            {
                throw new Exception("code length overflow. ("
                    + buffer.getLengthInBits()
                    + ">"
                    + totalDataCount * 8
                    + ")");
            }

            // end code
            if (buffer.getLengthInBits() + 4 <= totalDataCount * 8)
            {
                buffer.put(0, 4);
            }

            // padding
            while (buffer.getLengthInBits() % 8 != 0)
            {
                buffer.putBit(false);
            }

            // padding
            while (true)
            {

                if (buffer.getLengthInBits() >= totalDataCount * 8)
                {
                    break;
                }
                buffer.put(QRCode.PAD0, 8);

                if (buffer.getLengthInBits() >= totalDataCount * 8)
                {
                    break;
                }
                buffer.put(QRCode.PAD1, 8);
            }

            return createBytes(buffer, rsBlocks);
        }

        List<byte> createBytes(QRBitBuffer buffer, List<QRRSBlock> rsBlocks)
        {

            int offset = 0;

            int maxDcCount = 0;
            int maxEcCount = 0;

            var dcdata = new List<List<byte>>(rsBlocks.Count);
            var ecdata = new List<List<byte>>(rsBlocks.Count);

            for (int r = 0; r < rsBlocks.Count; r++)
            {

                var dcCount = rsBlocks[r].dataCount;
                var ecCount = rsBlocks[r].totalCount - dcCount;

                maxDcCount = Math.Max(maxDcCount, dcCount);
                maxEcCount = Math.Max(maxEcCount, ecCount);

                var l = new List<byte>();
                for (var i = 0; i < dcCount; i++)
                    l.Add(0);
                dcdata.Add( l);

                for (var i = 0; i < dcdata[r].Count; i++)
                {
                    dcdata[r][i] = (byte)(0xff & buffer.buffer[i + offset]);
                }
                offset += dcCount;

                var rsPoly = QRUtil.getErrorCorrectPolynomial(ecCount);
                var rawPoly = new QRPolynomial(dcdata[r], rsPoly.getLength() - 1);

                var modPoly = rawPoly.mod(rsPoly);
                var l2 = new List<byte>(rsPoly.getLength());
                for (var i = 0; i < rsPoly.getLength() - 1; i++)
                    l2.Add(0);
                ecdata.Add(l2);
                for (var i = 0; i < ecdata[r].Count; i++)
                {
                    var modIndex = i + modPoly.getLength() - ecdata[r].Count;
                    ecdata[r][i] = (byte)((modIndex >= 0) ? modPoly.get(modIndex) : 0);
                }

            }

            var totalCodeCount = 0;
            for (var i = 0; i < rsBlocks.Count; i++)
            {
                totalCodeCount += rsBlocks[i].totalCount;
            }

            var data = new List<byte>(totalCodeCount);
            for (var i = 0; i < totalCodeCount; i++)
                data.Add(0);
            var index = 0;

            for (var i = 0; i < maxDcCount; i++)
            {
                for (var r = 0; r < rsBlocks.Count; r++)
                {
                    if (i < dcdata[r].Count)
                    {
                        data[index++] = dcdata[r][i];
                    }
                }
            }

            for (var i = 0; i < maxEcCount; i++)
            {
                for (var r = 0; r < rsBlocks.Count; r++)
                {
                    if (i < ecdata[r].Count)
                    {
                        data[index++] = ecdata[r][i];
                    }
                }
            }

            return data;
        }
    }

    //---------------------------------------------------------------------
    /// <summary>
    /// 工具
    /// </summary>
    class QRUtil
    {
        static readonly byte[][] PATTERN_POSITION_TABLE = new byte[][] {
            new byte[]{},
            new byte[]{6, 18},
            new byte[]{6, 22},
            new byte[]{6, 26},
            new byte[]{6, 30},
            new byte[]{6, 34},
            new byte[]{6, 22, 38},
            new byte[]{6, 24, 42},
            new byte[]{6, 26, 46},
            new byte[]{6, 28, 50},
            new byte[]{6, 30, 54},
            new byte[]{6, 32, 58},
            new byte[]{6, 34, 62},
            new byte[]{6, 26, 46, 66},
            new byte[]{6, 26, 48, 70},
            new byte[]{6, 26, 50, 74},
            new byte[]{6, 30, 54, 78},
            new byte[]{6, 30, 56, 82},
            new byte[]{6, 30, 58, 86},
            new byte[]{6, 34, 62, 90},
            new byte[]{6, 28, 50, 72, 94},
            new byte[]{6, 26, 50, 74, 98},
            new byte[]{6, 30, 54, 78, 102},
            new byte[]{6, 28, 54, 80, 106},
            new byte[]{6, 32, 58, 84, 110},
            new byte[]{6, 30, 58, 86, 114},
            new byte[]{6, 34, 62, 90, 118},
            new byte[]{6, 26, 50, 74, 98, 122},
            new byte[]{6, 30, 54, 78, 102, 126},
            new byte[]{6, 26, 52, 78, 104, 130},
            new byte[]{6, 30, 56, 82, 108, 134},
            new byte[]{6, 34, 60, 86, 112, 138},
            new byte[]{6, 30, 58, 86, 114, 142},
            new byte[]{6, 34, 62, 90, 118, 146},
            new byte[]{6, 30, 54, 78, 102, 126, 150},
            new byte[]{6, 24, 50, 76, 102, 128, 154},
            new byte[]{6, 28, 54, 80, 106, 132, 158},
            new byte[]{6, 32, 58, 84, 110, 136, 162},
            new byte[]{6, 26, 54, 82, 110, 138, 166},
            new byte[]{6, 30, 58, 86, 114, 142, 170}
        };

        static int G15 = (1 << 10) | (1 << 8) | (1 << 5) | (1 << 4) | (1 << 2) | (1 << 1) | (1 << 0);
        static int G18 = (1 << 12) | (1 << 11) | (1 << 10) | (1 << 9) | (1 << 8) | (1 << 5) | (1 << 2) | (1 << 0);
        static int G15_MASK = (1 << 14) | (1 << 12) | (1 << 10) | (1 << 4) | (1 << 1);

        public static int getBCHTypeInfo(int data)
        {
            int d = data << 10;
            while (getBCHDigit(d) - getBCHDigit(G15) >= 0)
            {
                d ^= (G15 << (getBCHDigit(d) - getBCHDigit(G15)));
            }
            return ((data << 10) | d) ^ G15_MASK;
        }

        public static int getBCHTypeNumber(int data)
        {
            int d = data << 12;
            while (getBCHDigit(d) - getBCHDigit(G18) >= 0)
            {
                d ^= (G18 << (getBCHDigit(d) - getBCHDigit(G18)));
            }
            return (data << 12) | d;
        }

        public static byte getBCHDigit(int data)
        {
            byte digit = 0;
            while (data != 0)
            {
                digit++;
                data >>= 1;
            }
            return digit;
        }

        public static byte[] getPatternPosition(int typeNumber)
        {
            return PATTERN_POSITION_TABLE[typeNumber - 1];
        }

        public static bool getMask(QRMaskPattern maskPattern, int i, int j)
        {
            switch (maskPattern)
            {
                case QRMaskPattern.PATTERN000: return (i + j) % 2 == 0;
                case QRMaskPattern.PATTERN001: return i % 2 == 0;
                case QRMaskPattern.PATTERN010: return j % 3 == 0;
                case QRMaskPattern.PATTERN011: return (i + j) % 3 == 0;
                case QRMaskPattern.PATTERN100: return (Math.Floor((decimal)i / 2) + Math.Floor((decimal)j / 3)) % 2 == 0;
                case QRMaskPattern.PATTERN101: return (i * j) % 2 + (i * j) % 3 == 0;
                case QRMaskPattern.PATTERN110: return ((i * j) % 2 + (i * j) % 3) % 2 == 0;
                case QRMaskPattern.PATTERN111: return ((i * j) % 3 + (i + j) % 2) % 2 == 0;

                default:
                    throw new Exception("bad maskPattern:" + maskPattern);
            }
        }

        public static QRPolynomial getErrorCorrectPolynomial(int errorCorrectLength)
        {
            var a = new QRPolynomial(new List<byte> { 1 }, 0);

            for (var i = 0; i < errorCorrectLength; i++)
            {
                a = a.multiply(new QRPolynomial(new List<byte> { 1, QRMath.gexp(i) }, 0));
            }
            return a;
        }

        public static int getLengthInBits(QRMode mode, int type)
        {
            if (1 <= type && type < 10)
            {
                // 1 - 9
                switch (mode)
                {
                    case QRMode.MODE_NUMBER: return 10;
                    case QRMode.MODE_ALPHA_NUM: return 9;
                    case QRMode.MODE_8BIT_BYTE: return 8;
                    case QRMode.MODE_KANJI: return 8;
                    default:
                        throw new Exception("mode:" + mode);
                }

            }
            else if (type < 27)
            {
                // 10 - 26
                switch (mode)
                {
                    case QRMode.MODE_NUMBER: return 12;
                    case QRMode.MODE_ALPHA_NUM: return 11;
                    case QRMode.MODE_8BIT_BYTE: return 16;
                    case QRMode.MODE_KANJI: return 10;
                    default:
                        throw new Exception("mode:" + mode);
                }
            }
            else if (type < 41)
            {
                // 27 - 40
                switch (mode)
                {
                    case QRMode.MODE_NUMBER: return 14;
                    case QRMode.MODE_ALPHA_NUM: return 13;
                    case QRMode.MODE_8BIT_BYTE: return 16;
                    case QRMode.MODE_KANJI: return 12;
                    default:
                        throw new Exception("mode:" + mode);
                }
            }
            else
            {
                throw new Exception("type:" + type);
            }
        }

        public static int getLostPoint(QRCode qrCode)
        {
            int moduleCount = qrCode.getModuleCount();
            int lostPoint = 0;

            // LEVEL1

            for (int row = 0; row < moduleCount; row++)
            {
                for (int col = 0; col < moduleCount; col++)
                {
                    int sameCount = 0;
                    bool? dark = qrCode.isDark(row, col);

                    for (int r = -1; r <= 1; r++)
                    {
                        if (row + r < 0 || moduleCount <= row + r)
                        {
                            continue;
                        }

                        for (var c = -1; c <= 1; c++)
                        {
                            if (col + c < 0 || moduleCount <= col + c)
                            {
                                continue;
                            }

                            if (r == 0 && c == 0)
                            {
                                continue;
                            }

                            if (dark == qrCode.isDark(row + r, col + c))
                            {
                                sameCount++;
                            }
                        }
                    }

                    if (sameCount > 5)
                    {
                        lostPoint += (3 + sameCount - 5);
                    }
                }
            }

            // LEVEL2
            for (var row = 0; row < moduleCount - 1; row++)
            {
                for (var col = 0; col < moduleCount - 1; col++)
                {
                    var count = 0;
                    if (qrCode.isDark(row, col)==true) count++;
                    if (qrCode.isDark(row + 1, col) == true) count++;
                    if (qrCode.isDark(row, col + 1) == true) count++;
                    if (qrCode.isDark(row + 1, col + 1) == true) count++;
                    if (count == 0 || count == 4)
                    {
                        lostPoint += 3;
                    }
                }
            }

            // LEVEL3
            for (var row = 0; row < moduleCount; row++)
            {
                for (var col = 0; col < moduleCount - 6; col++)
                {
                    if (qrCode.isDark(row, col) == true
                            && (qrCode.isDark(row, col + 1)!= true)
                            && qrCode.isDark(row, col + 2) == true
                            && qrCode.isDark(row, col + 3) == true
                            && qrCode.isDark(row, col + 4) == true
                            && qrCode.isDark(row, col + 5)!=true
                            && qrCode.isDark(row, col + 6) == true)
                    {
                        lostPoint += 40;
                    }
                }
            }

            for (var col = 0; col < moduleCount; col++)
            {
                for (var row = 0; row < moduleCount - 6; row++)
                {
                    if (qrCode.isDark(row, col) == true
                            && qrCode.isDark(row + 1, col) != true
                            && qrCode.isDark(row + 2, col) == true
                            && qrCode.isDark(row + 3, col) == true
                            && qrCode.isDark(row + 4, col) == true
                            && qrCode.isDark(row + 5, col) != true
                            && qrCode.isDark(row + 6, col) == true)
                    {
                        lostPoint += 40;
                    }
                }
            }

            // LEVEL4
            var darkCount = 0;

            for (var col = 0; col < moduleCount; col++)
            {
                for (var row = 0; row < moduleCount; row++)
                {
                    if (qrCode.isDark(row, col) != true)
                    {
                        darkCount++;
                    }
                }
            }

            var ratio = Math.Abs(100 * darkCount / moduleCount / moduleCount - 50) / 5;
            lostPoint += ratio * 10;

            return lostPoint;
        }
    }

    //---------------------------------------------------------------------
    /// <summary>
    /// 计算
    /// </summary>
    class QRMath
    {
        static List<byte> EXP_TABLE = init();
        static List<byte> LOG_TABLE;
        static List<byte> init()
        {
            List<byte> EXP_TABLE = new List<byte>(256);
            LOG_TABLE = new List<byte>(256);
            for (byte i = 0; i < 8; i++)
            {
                LOG_TABLE.Add(0);
                EXP_TABLE.Add((byte)(1 << i));
            }
            for (var i = 8; i < 256; i++)
            {
                LOG_TABLE.Add(0);
                EXP_TABLE.Add( (byte)(EXP_TABLE[i - 4]
                    ^ EXP_TABLE[i - 5]
                    ^ EXP_TABLE[i - 6]
                    ^ EXP_TABLE[i - 8]));
            }
            for (byte i = 0; i < 255; i++)
            {
                LOG_TABLE[EXP_TABLE[i]] = i;
            }
            return EXP_TABLE;
        }
        public static byte glog(int n)
        {
            if (n < 1)
            {
                throw new Exception("参数过小 " + n);
            }

            return QRMath.LOG_TABLE[n];
        }

        public static byte gexp(int n)
        {
            while (n < 0)
            {
                n += 255;
            }

            while (n >= 256)
            {
                n -= 255;
            }

            return QRMath.EXP_TABLE[n];
        }
    }

    //---------------------------------------------------------------------
    /// <summary>
    /// 多项式
    /// </summary>
    class QRPolynomial
    {
        List<byte> num;
        public QRPolynomial(List<byte> num, int shift)
        {
            if (num == null)
            {
                throw new Exception("参数错误 num,shift");
            }

            int offset = 0;

            while (offset < num.Count && num[offset] == 0)
            {
                offset++;
            }

            this.num = new List<byte>(num.Count - offset + shift);
            for (var i = 0; i < (num.Count - offset + shift); i++)
                this.num.Add(0);
            for (var i = 0; i < num.Count - offset; i++)
            {
                this.num[i]= num[i + offset];
            }
        }

        public byte get(int index)
        {
            return this.num[index];
        }

        public int getLength()
        {
            return this.num.Count;
        }

        public QRPolynomial multiply(QRPolynomial e)
        {
            int count = this.getLength() + e.getLength() - 1;
            var num = new List<byte>(count);
            for (int i = 0; i < count; i++)
                num.Add(0);

            for (var i = 0; i < this.getLength(); i++)
            {
                for (var j = 0; j < e.getLength(); j++)
                {
                    byte b1 = QRMath.glog(this.get(i));
                    byte b2 = QRMath.glog(e.get(j));
                    num[i + j] ^= QRMath.gexp(b1 + b2);
                }
            }

            return new QRPolynomial(num, 0);
        }

        public QRPolynomial mod(QRPolynomial e)
        {
            if (this.getLength() - e.getLength() < 0)
            {
                return this;
            }
            var ratio = QRMath.glog(this.get(0)) - QRMath.glog(e.get(0));

            var num = new List<byte>(this.getLength());
            for (var i = 0; i < this.getLength(); i++)
            {
                num.Add(this.get(i));
            }

            for (var i = 0; i < e.getLength(); i++)
            {
                num[i] ^= QRMath.gexp(QRMath.glog(e.get(i)) + ratio);
            }
            // recursive call
            return new QRPolynomial(num, 0).mod(e);
        }
    }

    //---------------------------------------------------------------------
    /// <summary>
    /// 资源块
    /// </summary>
    class QRRSBlock
    {
        public int totalCount;
        public int dataCount;

        public QRRSBlock(int totalCount, int dataCount)
        {
            this.totalCount = totalCount;
            this.dataCount = dataCount;
        }

        static byte[][] RS_BLOCK_TABLE = makeRS_BLOCK_TABLE();

        static byte[][] makeRS_BLOCK_TABLE()
        {
            byte[][] r = new byte[][] {
            // L
            // M
            // Q
            // H

            // 1
            new byte[] {1, 26, 19},
            new byte[] {1, 26, 16},
            new byte[] {1, 26, 13},
            new byte[] {1, 26, 9},
	
	        // 2
            new byte[] {1, 44, 34},
            new byte[] {1, 44, 28},
            new byte[] {1, 44, 22},
            new byte[] {1, 44, 16},
	
	        // 3
            new byte[] {1, 70, 55},
            new byte[] {1, 70, 44},
            new byte[] {2, 35, 17},
            new byte[] {2, 35, 13},
	
	        // 4
            new byte[] {1, 100, 80},
            new byte[] {2, 50, 32},
            new byte[] {2, 50, 24},
            new byte[] {4, 25, 9},
	
	        // 5
            new byte[] {1, 134, 108},
            new byte[] {2, 67, 43},
            new byte[] {2, 33, 15, 2, 34, 16},
            new byte[] {2, 33, 11, 2, 34, 12},
	
	        // 6
            new byte[] {2, 86, 68},
            new byte[] {4, 43, 27},
            new byte[] {4, 43, 19},
            new byte[] {4, 43, 15},
	
	        // 7
            new byte[] {2, 98, 78},
            new byte[] {4, 49, 31},
            new byte[] {2, 32, 14, 4, 33, 15},
            new byte[] {4, 39, 13, 1, 40, 14},
	
	        // 8
            new byte[] {2, 121, 97},
            new byte[] {2, 60, 38, 2, 61, 39},
            new byte[] {4, 40, 18, 2, 41, 19},
            new byte[] {4, 40, 14, 2, 41, 15},
	
	        // 9
            new byte[] {2, 146, 116},
            new byte[] {3, 58, 36, 2, 59, 37},
            new byte[] {4, 36, 16, 4, 37, 17},
            new byte[] {4, 36, 12, 4, 37, 13},
	
	        // 10
            new byte[] {2, 86, 68, 2, 87, 69},
            new byte[] {4, 69, 43, 1, 70, 44},
            new byte[] {6, 43, 19, 2, 44, 20},
            new byte[] {6, 43, 15, 2, 44, 16},
	
	        // 11
            new byte[] {4, 101, 81},
            new byte[] {1, 80, 50, 4, 81, 51},
            new byte[] {4, 50, 22, 4, 51, 23},
            new byte[] {3, 36, 12, 8, 37, 13},
	
	        // 12
            new byte[] {2, 116, 92, 2, 117, 93},
            new byte[] {6, 58, 36, 2, 59, 37},
            new byte[] {4, 46, 20, 6, 47, 21},
            new byte[] {7, 42, 14, 4, 43, 15},
	
	        // 13
            new byte[] {4, 133, 107},
            new byte[] {8, 59, 37, 1, 60, 38},
            new byte[] {8, 44, 20, 4, 45, 21},
            new byte[] {12, 33, 11, 4, 34, 12},
	
	        // 14
            new byte[] {3, 145, 115, 1, 146, 116},
            new byte[] {4, 64, 40, 5, 65, 41},
            new byte[] {11, 36, 16, 5, 37, 17},
            new byte[] {11, 36, 12, 5, 37, 13},
	
	        // 15
            new byte[] {5, 109, 87, 1, 110, 88},
            new byte[] {5, 65, 41, 5, 66, 42},
            new byte[] {5, 54, 24, 7, 55, 25},
            new byte[] {11, 36, 12},
	
	        // 16
            new byte[] {5, 122, 98, 1, 123, 99},
            new byte[] {7, 73, 45, 3, 74, 46},
            new byte[] {15, 43, 19, 2, 44, 20},
            new byte[] {3, 45, 15, 13, 46, 16},
	
	        // 17
            new byte[] {1, 135, 107, 5, 136, 108},
            new byte[] {10, 74, 46, 1, 75, 47},
            new byte[] {1, 50, 22, 15, 51, 23},
            new byte[] {2, 42, 14, 17, 43, 15},
	
	        // 18
            new byte[] {5, 150, 120, 1, 151, 121},
            new byte[] {9, 69, 43, 4, 70, 44},
            new byte[] {17, 50, 22, 1, 51, 23},
            new byte[] {2, 42, 14, 19, 43, 15},
	
	        // 19
            new byte[] {3, 141, 113, 4, 142, 114},
            new byte[] {3, 70, 44, 11, 71, 45},
            new byte[] {17, 47, 21, 4, 48, 22},
            new byte[] {9, 39, 13, 16, 40, 14},
	
	        // 20
            new byte[] {3, 135, 107, 5, 136, 108},
            new byte[] {3, 67, 41, 13, 68, 42},
            new byte[] {15, 54, 24, 5, 55, 25},
            new byte[] {15, 43, 15, 10, 44, 16},
	
	        // 21
            new byte[] {4, 144, 116, 4, 145, 117},
            new byte[] {17, 68, 42},
            new byte[] {17, 50, 22, 6, 51, 23},
            new byte[] {19, 46, 16, 6, 47, 17},
	
	        // 22
            new byte[] {2, 139, 111, 7, 140, 112},
            new byte[] {17, 74, 46},
            new byte[] {7, 54, 24, 16, 55, 25},
            new byte[] {34, 37, 13},
	
	        // 23
            new byte[] {4, 151, 121, 5, 152, 122},
            new byte[] {4, 75, 47, 14, 76, 48},
            new byte[] {11, 54, 24, 14, 55, 25},
            new byte[] {16, 45, 15, 14, 46, 16},
	
	        // 24
            new byte[] {6, 147, 117, 4, 148, 118},
            new byte[] {6, 73, 45, 14, 74, 46},
            new byte[] {11, 54, 24, 16, 55, 25},
            new byte[] {30, 46, 16, 2, 47, 17},
	
	        // 25
            new byte[] {8, 132, 106, 4, 133, 107},
            new byte[] {8, 75, 47, 13, 76, 48},
            new byte[] {7, 54, 24, 22, 55, 25},
            new byte[] {22, 45, 15, 13, 46, 16},
	
	        // 26
            new byte[] {10, 142, 114, 2, 143, 115},
            new byte[] {19, 74, 46, 4, 75, 47},
            new byte[] {28, 50, 22, 6, 51, 23},
            new byte[] {33, 46, 16, 4, 47, 17},
	
	        // 27
            new byte[] {8, 152, 122, 4, 153, 123},
            new byte[] {22, 73, 45, 3, 74, 46},
            new byte[] {8, 53, 23, 26, 54, 24},
            new byte[] {12, 45, 15, 28, 46, 16},
	
	        // 28
            new byte[] {3, 147, 117, 10, 148, 118},
            new byte[] {3, 73, 45, 23, 74, 46},
            new byte[] {4, 54, 24, 31, 55, 25},
            new byte[] {11, 45, 15, 31, 46, 16},
	
	        // 29
            new byte[] {7, 146, 116, 7, 147, 117},
            new byte[] {21, 73, 45, 7, 74, 46},
            new byte[] {1, 53, 23, 37, 54, 24},
            new byte[] {19, 45, 15, 26, 46, 16},
	
	        // 30
            new byte[] {5, 145, 115, 10, 146, 116},
            new byte[] {19, 75, 47, 10, 76, 48},
            new byte[] {15, 54, 24, 25, 55, 25},
            new byte[] {23, 45, 15, 25, 46, 16},
	
	        // 31
            new byte[] {13, 145, 115, 3, 146, 116},
            new byte[] {2, 74, 46, 29, 75, 47},
            new byte[] {42, 54, 24, 1, 55, 25},
            new byte[] {23, 45, 15, 28, 46, 16},
	
	        // 32
            new byte[] {17, 145, 115},
            new byte[] {10, 74, 46, 23, 75, 47},
            new byte[] {10, 54, 24, 35, 55, 25},
            new byte[] {19, 45, 15, 35, 46, 16},
	
	        // 33
            new byte[] {17, 145, 115, 1, 146, 116},
            new byte[] {14, 74, 46, 21, 75, 47},
            new byte[] {29, 54, 24, 19, 55, 25},
            new byte[] {11, 45, 15, 46, 46, 16},
	
	        // 34
            new byte[] {13, 145, 115, 6, 146, 116},
            new byte[] {14, 74, 46, 23, 75, 47},
            new byte[] {44, 54, 24, 7, 55, 25},
            new byte[] {59, 46, 16, 1, 47, 17},
	
	        // 35
            new byte[] {12, 151, 121, 7, 152, 122},
            new byte[] {12, 75, 47, 26, 76, 48},
            new byte[] {39, 54, 24, 14, 55, 25},
            new byte[] {22, 45, 15, 41, 46, 16},
	
	        // 36
            new byte[] {6, 151, 121, 14, 152, 122},
            new byte[] {6, 75, 47, 34, 76, 48},
            new byte[] {46, 54, 24, 10, 55, 25},
            new byte[] {2, 45, 15, 64, 46, 16},
	
	        // 37
            new byte[] {17, 152, 122, 4, 153, 123},
            new byte[] {29, 74, 46, 14, 75, 47},
            new byte[] {49, 54, 24, 10, 55, 25},
            new byte[] {24, 45, 15, 46, 46, 16},
	
	        // 38
            new byte[] {4, 152, 122, 18, 153, 123},
            new byte[] {13, 74, 46, 32, 75, 47},
            new byte[] {48, 54, 24, 14, 55, 25},
            new byte[] {42, 45, 15, 32, 46, 16},
	
	        // 39
            new byte[] {20, 147, 117, 4, 148, 118},
            new byte[] {40, 75, 47, 7, 76, 48},
            new byte[] {43, 54, 24, 22, 55, 25},
            new byte[] {10, 45, 15, 67, 46, 16},
	
	        // 40
            new byte[] {19, 148, 118, 6, 149, 119},
            new byte[] {18, 75, 47, 31, 76, 48},
            new byte[] {34, 54, 24, 34, 55, 25},
            new byte[] {20, 45, 15, 61, 46, 16}
            };
            return r;
        }

        public static List<QRRSBlock> getRSBlocks(int typeNumber, QRErrorCorrectLevel errorCorrectLevel)
        {
            var rsBlock = getRsBlockTable(typeNumber, errorCorrectLevel);

            if (rsBlock == null)
            {
                throw new Exception("bad rs block @ typeNumber:" + typeNumber + "/errorCorrectLevel:" + errorCorrectLevel);
            }
            var length = rsBlock.Length / 3;
            var list = new List<QRRSBlock>(length);

            for (var i = 0; i < length; i++)
            {
                byte count = rsBlock[i * 3 + 0];
                byte totalCount = rsBlock[i * 3 + 1];
                byte dataCount = rsBlock[i * 3 + 2];

                for (var j = 0; j < count; j++)
                {
                    list.Add(new QRRSBlock(totalCount, dataCount));
                }
            }
            return list;
        }

        public static byte[] getRsBlockTable(int typeNumber, QRErrorCorrectLevel errorCorrectLevel)
        {
            switch (errorCorrectLevel)
            {
                case QRErrorCorrectLevel.L:
                    return QRRSBlock.RS_BLOCK_TABLE[(typeNumber - 1) * 4 + 0];
                case QRErrorCorrectLevel.M:
                    return QRRSBlock.RS_BLOCK_TABLE[(typeNumber - 1) * 4 + 1];
                case QRErrorCorrectLevel.Q:
                    return QRRSBlock.RS_BLOCK_TABLE[(typeNumber - 1) * 4 + 2];
                case QRErrorCorrectLevel.H:
                    return QRRSBlock.RS_BLOCK_TABLE[(typeNumber - 1) * 4 + 3];
                default:
                    return null;
            }
        }
    }

    //---------------------------------------------------------------------
    /// <summary>
    /// 位缓冲区
    /// </summary>
    class QRBitBuffer
    {
        public List<byte> buffer;
        int length;

        public QRBitBuffer() {
            buffer = new List<byte>();
            length = 0;
        }

        public bool get(int index)
        {
            int bufIndex = (int)Math.Floor((decimal)index / 8);
            return ((this.buffer[bufIndex] >> (7 - index % 8)) & 1) == 1;
        }

        public void put(int num, int length)
        {
            for (var i = 0; i < length; i++)
            {
                putBit(((num >> (length - i - 1)) & 1) == 1);
            }
        }

        public int getLengthInBits()
        {
            return length;
        }

        public void putBit(bool bit)
        {
            int bufIndex = (int)Math.Floor((decimal)length / 8);
            if (buffer.Count <= bufIndex)
            {
                buffer.Add(0);
            }
            if (bit)
            {
                buffer[bufIndex] |= (byte)(0x80 >> (length % 8));
            }
            length++;
        }
    }
}
