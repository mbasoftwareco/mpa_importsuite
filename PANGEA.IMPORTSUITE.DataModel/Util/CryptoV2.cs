
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace PANGEA.IMPORTSUITE.DataModel.Util
{
    public class CryptoV2
    {
        #region Declaracion de variables
        byte[] Key = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 }; //clave generada de arreglo de 8 numeros 1-8
        byte[] IV = { 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 }; //clave generada de arreglo de 8 numeros 9-16
        private SymmetricAlgorithm mCSP;

        #endregion
        #region Clase

        public CryptoV2()
        {
            try
            {
                mCSP = SetEnc();
                mCSP.Key = Key;
                mCSP.IV = IV;
            }
            catch
            {
                mCSP.Key = Key;
                mCSP.IV = IV;
            }

        }

        #endregion
        #region Funciones
        public string Encriptar(string Value)
        {
            ICryptoTransform ct;
            MemoryStream ms;
            CryptoStream cs;
            byte[] byt;

            ct = mCSP.CreateEncryptor(mCSP.Key, mCSP.IV);

            byt = Encoding.UTF8.GetBytes(Value);

            ms = new MemoryStream();
            cs = new CryptoStream(ms, ct, CryptoStreamMode.Write);
            cs.Write(byt, 0, byt.Length);
            cs.FlushFinalBlock();

            cs.Close();

            return Convert.ToBase64String(ms.ToArray());
        }

        public string DesEncriptar(string Value)
        {
            ICryptoTransform ct;
            MemoryStream ms;
            CryptoStream cs;
            byte[] byt;

            ct = mCSP.CreateDecryptor(mCSP.Key, mCSP.IV);

            byt = Convert.FromBase64String(Value);

            ms = new MemoryStream();
            cs = new CryptoStream(ms, ct, CryptoStreamMode.Write);
            cs.Write(byt, 0, byt.Length);
            cs.FlushFinalBlock();

            cs.Close();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private SymmetricAlgorithm SetEnc()
        {
            return new DESCryptoServiceProvider();
        }
        #endregion
    }
}

