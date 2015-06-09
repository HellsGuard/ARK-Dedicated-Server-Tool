Public Class IniFile
    Private Declare Ansi Function GetPrivateProfileString _
      Lib "kernel32.dll" Alias "GetPrivateProfileStringA" _
      (ByVal lpApplicationName As String, _
      ByVal lpKeyName As String, ByVal lpDefault As String, _
      ByVal lpReturnedString As System.Text.StringBuilder, _
      ByVal nSize As Integer, ByVal lpFileName As String) _
      As Integer
    Private Declare Ansi Function WritePrivateProfileString _
      Lib "kernel32.dll" Alias "WritePrivateProfileStringA" _
      (ByVal lpApplicationName As String, _
      ByVal lpKeyName As String, ByVal lpString As String, _
      ByVal lpFileName As String) As Integer
    Private Declare Ansi Function GetPrivateProfileInt _
      Lib "kernel32.dll" Alias "GetPrivateProfileIntA" _
      (ByVal lpApplicationName As String, _
      ByVal lpKeyName As String, ByVal nDefault As Integer, _
      ByVal lpFileName As String) As Integer
    Private Declare Ansi Function FlushPrivateProfileString _
      Lib "kernel32.dll" Alias "WritePrivateProfileStringA" _
      (ByVal lpApplicationName As Integer, _
      ByVal lpKeyName As Integer, ByVal lpString As Integer, _
      ByVal lpFileName As String) As Integer
    Dim strFilename As String

    Public Sub New(ByVal Filename As String)
        strFilename = Filename
    End Sub

    ReadOnly Property FileName() As String
        Get
            Return strFilename
        End Get
    End Property

    Public Function GetString(ByVal Section As String, ByVal Key As String, ByVal [Default] As String) As String
        Dim intCharCount As Integer
        Dim objResult As New System.Text.StringBuilder(256)
        intCharCount = GetPrivateProfileString(Section, Key, [Default], objResult, objResult.Capacity, strFilename)
        If intCharCount > 0 Then GetString = Left(objResult.ToString, intCharCount)
    End Function

    Public Function GetInteger(ByVal Section As String, ByVal Key As String, ByVal [Default] As Integer) As Integer
        Return GetPrivateProfileInt(Section, Key, [Default], strFilename)
    End Function

    Public Function GetBoolean(ByVal Section As String, ByVal Key As String, ByVal [Default] As Boolean) As Boolean
        If [Default] = True Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Sub WriteString(ByVal Section As String, ByVal Key As String, ByVal Value As String)
        WritePrivateProfileString(Section, Key, Value, strFilename)
        Flush()
    End Sub

    Public Sub WriteInteger(ByVal Section As String, ByVal Key As String, ByVal Value As Integer)
        WriteString(Section, Key, CStr(Value))
        Flush()
    End Sub

    Public Sub WriteBoolean(ByVal Section As String, ByVal Key As String, ByVal Value As Boolean)
        WriteString(Section, Key, Value)
        Flush()
    End Sub

    Private Sub Flush()
        FlushPrivateProfileString(0, 0, 0, strFilename)
    End Sub
End Class

