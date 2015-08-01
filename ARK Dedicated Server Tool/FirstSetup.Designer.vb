<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FirstSetup
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FirstSetup))
        Me.btnDefaultSetup = New System.Windows.Forms.Button()
        Me.btnAdvancedSeup = New System.Windows.Forms.Button()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.SuspendLayout()
        '
        'btnDefaultSetup
        '
        Me.btnDefaultSetup.Location = New System.Drawing.Point(21, 12)
        Me.btnDefaultSetup.Name = "btnDefaultSetup"
        Me.btnDefaultSetup.Size = New System.Drawing.Size(82, 23)
        Me.btnDefaultSetup.TabIndex = 0
        Me.btnDefaultSetup.Text = "Default Setup"
        Me.btnDefaultSetup.UseVisualStyleBackColor = True
        '
        'btnAdvancedSeup
        '
        Me.btnAdvancedSeup.Location = New System.Drawing.Point(12, 62)
        Me.btnAdvancedSeup.Name = "btnAdvancedSeup"
        Me.btnAdvancedSeup.Size = New System.Drawing.Size(100, 23)
        Me.btnAdvancedSeup.TabIndex = 1
        Me.btnAdvancedSeup.Text = "Advanced Setup"
        Me.btnAdvancedSeup.UseVisualStyleBackColor = True
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(125, 12)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(318, 26)
        Me.Label1.TabIndex = 2
        Me.Label1.Text = "Use Default Setup if you don't wish to mess with folders or have a " & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "pre-existing" & _
    " installation that you don't wish to modify." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10)
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(125, 62)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(275, 39)
        Me.Label2.TabIndex = 3
        Me.Label2.Text = "Use Advanced Setup if you wish to specify where to " & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "install your server, or if y" & _
    "ou have a pre-existing installation" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "that you wish to modify."
        '
        'FirstSetup
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(443, 110)
        Me.ControlBox = False
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.btnAdvancedSeup)
        Me.Controls.Add(Me.btnDefaultSetup)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximumSize = New System.Drawing.Size(459, 148)
        Me.MinimumSize = New System.Drawing.Size(459, 148)
        Me.Name = "FirstSetup"
        Me.Text = "ARK: Survival Evolved - Server Tool Initial Setup"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents btnDefaultSetup As System.Windows.Forms.Button
    Friend WithEvents btnAdvancedSeup As System.Windows.Forms.Button
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
End Class
