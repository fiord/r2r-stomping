Imports System
Imports System.Security.Cryptography
Imports System.Text

Module Program
    ' AES-256 fixed key (32 bytes)
    Private ReadOnly Key As Byte() = {
        &H2B, &H7E, &H15, &H16, &H28, &HAE, &HD2, &HA6,
        &HAB, &HF7, &H15, &H88, &H09, &HCF, &H4F, &H3C,
        &H76, &H2E, &H71, &H60, &HF3, &H8B, &H4D, &HA5,
        &H6A, &H78, &H4D, &H90, &H45, &H19, &H0C, &HFE
    }

    ' AES fixed IV (16 bytes)
    Private ReadOnly IV As Byte() = {
        &H00, &H01, &H02, &H03, &H04, &H05, &H06, &H07,
        &H08, &H09, &H0A, &H0B, &H0C, &H0D, &H0E, &H0F
    }

    Sub Main(args As String())
        Dim input As String
        If args.Length > 0 Then
            input = String.Join(" ", args)
        Else
            input = If(Console.ReadLine(), String.Empty)
        End If

        Dim plaintext As Byte() = Encoding.UTF8.GetBytes(input)

        Using aes As Aes = Aes.Create()
            aes.Key = Key
            aes.IV = IV
            aes.Mode = CipherMode.CBC
            aes.Padding = PaddingMode.PKCS7

            Using encryptor As ICryptoTransform = aes.CreateEncryptor()
                Dim ciphertext As Byte() = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length)
                Console.WriteLine(Convert.ToBase64String(ciphertext))
            End Using
        End Using
    End Sub
End Module
