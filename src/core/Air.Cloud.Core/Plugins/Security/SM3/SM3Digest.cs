/*
 * Copyright (c) 2024-2030 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
namespace Air.Cloud.Core.Plugins.Security.SM3
{
    /// <summary>
    ///  
    /// ⊕ 等价于 ^
    /// ^ 等价于 &
    /// v 等价于 |
    /// </summary>
    internal class SM3Digest : GeneralDigest
    {
        public override string AlgorithmName
        {
            get
            {
                return "SM3";
            }

        }
        /// <summary>
        /// 消息摘要生成的摘要的大小
        /// </summary>
        /// <returns></returns>
        public override int GetDigestSize()
        {
            return DigestLength;
        }
        /// <summary>
        /// SM3算法产生的哈希值大小
        /// </summary>
        private const int DigestLength = 32;

        /// <summary>
        /// 初始值IV
        /// </summary>
        private static readonly int[] IV = new int[] {
            0x7380166f, 0x4914b2b9, 0x172442d7,
            unchecked((int)0xda8a0600), unchecked((int)0xa96f30bc), 0x163138aa,
            unchecked((int)0xe38dee4d), unchecked((int)0xb0fb0e4e)
        };
        /// <summary>
        /// 备份的字寄存器
        /// </summary>
        private readonly int[] v = new int[8];
        /// <summary>
        /// 使用中的字寄存器
        /// </summary>
        private readonly int[] v_ = new int[8];

        private static readonly int[] X0 = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        private readonly int[] X = new int[68];
        private int xOff;

        /// <summary>
        /// 0到15的Tj常量
        /// </summary>
        private readonly int TOne = 0x79cc4519;
        /// <summary>
        /// 16到63的Tj常量
        /// </summary>
        private readonly int TSecond = 0x7a879d8a;

        public SM3Digest()
        {
            Reset();
        }
        /// <summary>
        /// 复制构造函数
        /// </summary>
        /// <param name="t"></param>
        public SM3Digest(SM3Digest t) : base(t)
        {

            Array.Copy(t.X, 0, X, 0, t.X.Length);
            xOff = t.xOff;

            Array.Copy(t.v, 0, v, 0, t.v.Length);
        }
        /// <summary>
        /// 将复制的对象状态还原到该对象。
        /// 此方法的实现应尝试避免或最小化内存分配以执行重置。
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Array.Copy(IV, 0, v, 0, IV.Length);

            xOff = 0;
            Array.Copy(X0, 0, X, 0, X0.Length);
        }

        internal override void ProcessBlock()
        {
            int j;

            int[] ww = X;
            //64位比特串
            int[] ww_ = new int[64];

            #region 块消息扩展
            //消息扩展16 TO 67
            for (j = 16; j < 68; j++)
            {
                ww[j] = P1(ww[j - 16] ^ ww[j - 9] ^ Rotate(ww[j - 3], 15)) ^ Rotate(ww[j - 13], 7) ^ ww[j - 6];
            }
            //消息扩展0 TO 63
            for (j = 0; j < 64; j++)
            {
                ww_[j] = ww[j] ^ ww[j + 4];
            }
            #endregion

            #region 压缩函数
            int[] vv = v;
            int[] vv_ = v_;//A,B,C,D,E,F,G,H为字寄存器

            Array.Copy(vv, 0, vv_, 0, IV.Length);
            //中间变量SS1,SS2,TT1,TT2
            int SS1, SS2, TT1, TT2;
            int aaa;
            //将消息分组B(i)划分为16个字
            for (j = 0; j < 16; j++)
            {
                aaa = Rotate(vv_[0], 12);
                SS1 = aaa + vv_[4] + Rotate(TOne, j);
                SS1 = Rotate(SS1, 7);
                SS2 = SS1 ^ aaa;

                TT1 = FFOne(vv_[0], vv_[1], vv_[2]) + vv_[3] + SS2 + ww_[j];
                TT2 = GGOne(vv_[4], vv_[5], vv_[6]) + vv_[7] + SS1 + ww[j];

                #region 更新各个寄存器
                vv_[3] = vv_[2];
                vv_[2] = Rotate(vv_[1], 9);
                vv_[1] = vv_[0];
                vv_[0] = TT1;
                vv_[7] = vv_[6];
                vv_[6] = Rotate(vv_[5], 19);
                vv_[5] = vv_[4];
                vv_[4] = P0(TT2);
                #endregion
            }

            for (j = 16; j < 64; j++)
            {
                aaa = Rotate(vv_[0], 12);
                SS1 = aaa + vv_[4] + Rotate(TSecond, j);
                SS1 = Rotate(SS1, 7);
                SS2 = SS1 ^ aaa;

                TT1 = FFSecond(vv_[0], vv_[1], vv_[2]) + vv_[3] + SS2 + ww_[j];
                TT2 = GGSecond(vv_[4], vv_[5], vv_[6]) + vv_[7] + SS1 + ww[j];

                #region 更新各个寄存器
                vv_[3] = vv_[2];
                vv_[2] = Rotate(vv_[1], 9);
                vv_[1] = vv_[0];
                vv_[0] = TT1;
                vv_[7] = vv_[6];
                vv_[6] = Rotate(vv_[5], 19);
                vv_[5] = vv_[4];
                vv_[4] = P0(TT2);
                #endregion
            }
            #endregion

            //256比特的杂凑值y =vv_(j+1) ABCDEFGH
            for (j = 0; j < 8; j++)
            {
                vv[j] ^= vv_[j];
            }

            // Reset
            xOff = 0;
            Array.Copy(X0, 0, X, 0, X0.Length);
        }

        internal override void ProcessWord(byte[] in_Renamed, int inOff)
        {
            int n = in_Renamed[inOff] << 24;
            n |= (in_Renamed[++inOff] & 0xff) << 16;
            n |= (in_Renamed[++inOff] & 0xff) << 8;
            n |= in_Renamed[++inOff] & 0xff;
            X[xOff] = n;

            if (++xOff == 16)
            {
                ProcessBlock();
            }
        }

        internal override void ProcessLength(long bitLength)
        {
            if (xOff > 14)
            {
                ProcessBlock();
            }

            X[14] = (int)SM3Util.URShift(bitLength, 32);
            X[15] = (int)(bitLength & unchecked((int)0xffffffff));
        }

        /// <summary>
        /// 写入到大端
        /// </summary>
        /// <param name="n"></param>
        /// <param name="bs"></param>
        /// <param name="off"></param>
        public static void IntToBigEndian(int n, byte[] bs, int off)
        {
            bs[off] = (byte)SM3Util.URShift(n, 24);
            bs[++off] = (byte)SM3Util.URShift(n, 16);
            bs[++off] = (byte)SM3Util.URShift(n, 8);
            bs[++off] = (byte)n;
        }
        /// <summary>
        /// 关闭摘要，产生最终的摘要值。doFinal调用使摘要复位。
        /// </summary>
        /// <param name="out_Renamed"></param>
        /// <param name="outOff"></param>
        /// <returns></returns>
        public override int DoFinal(byte[] out_Renamed, int outOff)
        {
            Finish();
            for (int i = 0; i < 8; i++)
            {
                IntToBigEndian(v[i], out_Renamed, outOff + i * 4);
            }
            Reset();
            return DigestLength;
        }

        /// <summary>
        /// x循环左移n比特运算
        /// </summary>
        /// <param name="x"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        private static int Rotate(int x, int n)
        {
            return x << n | SM3Util.URShift(x, 32 - n);
        }

        #region 置换函数
        /// <summary>
        /// 置换函数P0
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private static int P0(int x)
        {
            return x ^ Rotate(x, 9) ^ Rotate(x, 17);
        }
        /// <summary>
        /// 置换函数P1
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private static int P1(int x)
        {
            return x ^ Rotate(x, 15) ^ Rotate(x, 23);
        }
        #endregion

        #region 布尔函数
        /// <summary>
        /// 0到15的布尔函数FF (X⊕^Y⊕Z)
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        /// <returns></returns>
        private static int FFOne(int X, int Y, int Z)
        {
            return X ^ Y ^ Z;
        }
        /// <summary>
        /// 16到63的布尔函数FF
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        /// <returns></returns>
        private static int FFSecond(int X, int Y, int Z)
        {
            return X & Y | X & Z | Y & Z;
        }

        /// <summary>
        /// 0到15的布尔函数GG
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        /// <returns></returns>
        private static int GGOne(int X, int Y, int Z)
        {
            return X ^ Y ^ Z;
        }
        /// <summary>
        /// 16到63的布尔函数GG
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        /// <returns></returns>
        private static int GGSecond(int X, int Y, int Z)
        {
            return X & Y | ~X & Z;
        }
        #endregion
    }
}
