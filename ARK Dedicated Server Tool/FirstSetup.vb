Public Class FirstSetup
    Dim path As String

    Private Sub FirstSetup_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If My.Settings.firstTimeRan Then
            Me.Close()
        End If
    End Sub

    Private Sub btnDefaultSetup_Click(sender As Object, e As EventArgs) Handles btnDefaultSetup.Click
        My.Settings.defaultSetup = True
        My.Settings.firstTimeRan = True
        My.Settings.userSetDirectoryBool = False
        My.Settings.Save()

        Main.DefaultSetup()
        Me.Close()
    End Sub

    Private Sub btnAdvancedSeup_Click(sender As Object, e As EventArgs) Handles btnAdvancedSeup.Click
        My.Settings.advancedSetup = True
        My.Settings.firstTimeRan = True
        My.Settings.userSetDirectoryBool = True
        My.Settings.Save()

        Main.AdvancedSetup()
        Me.Close()
    End Sub
End Class