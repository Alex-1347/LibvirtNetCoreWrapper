Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports Microsoft.VisualBasic

Partial Module Program

    Public Function EncryptString(ByVal Text As String, ByVal KeyString As String) As String
        Dim Key As Byte() = Encoding.UTF8.GetBytes(KeyString)
        ReDim Preserve Key(31)
        Using AesAlg = Aes.Create()
            Using Encryptor = AesAlg.CreateEncryptor(Key, AesAlg.IV)
                Using MsEncrypt = New MemoryStream()
                    Using CsEncrypt = New CryptoStream(MsEncrypt, Encryptor, CryptoStreamMode.Write)
                        Using SwEncrypt = New StreamWriter(CsEncrypt)
                            SwEncrypt.Write(Text)
                        End Using
                    End Using
                    Dim IV = AesAlg.IV
                    Dim DecryptedContent = MsEncrypt.ToArray()
                    Dim Result = New Byte(IV.Length + DecryptedContent.Length - 1) {}
                    Buffer.BlockCopy(IV, 0, Result, 0, IV.Length)
                    Buffer.BlockCopy(DecryptedContent, 0, Result, IV.Length, DecryptedContent.Length)
                    Return Convert.ToBase64String(Result)
                End Using
            End Using
        End Using
    End Function

    Public Function DecryptString(ByVal CipherText As String, ByVal KeyString As String) As String
        Try
            Dim FullCipher = Convert.FromBase64String(CipherText)
            Dim IV = New Byte(15) {}
            Dim Cipher = New Byte(15) {}
            Buffer.BlockCopy(FullCipher, 0, IV, 0, IV.Length)
            Buffer.BlockCopy(FullCipher, IV.Length, Cipher, 0, IV.Length)
            Dim Key = Encoding.UTF8.GetBytes(KeyString)
            ReDim Preserve Key(31)
            Using AesAlg = Aes.Create()
                Using Decryptor = AesAlg.CreateDecryptor(Key, IV)
                    Dim Result As String
                    Using MsDecrypt = New MemoryStream(Cipher)
                        Using CsDecrypt = New CryptoStream(MsDecrypt, Decryptor, CryptoStreamMode.Read)
                            Using SrDecrypt = New StreamReader(CsDecrypt)
                                Result = SrDecrypt.ReadToEnd()
                            End Using
                        End Using
                    End Using
                    Return Result
                End Using
            End Using
        Catch ex As System.Security.Cryptography.CryptographicException
            Return "Wrong password"
        End Try

    End Function


End Module
