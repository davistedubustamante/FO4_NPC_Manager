Option Strict On
Imports System.IO
Namespace ExperimentalTransformation
    Public Module ControlledEspOutput
        Public Sub Emit(path As String, writer As Action(Of Stream))
            If ControlledFileOutput.IsActive Then
                ControlledFileOutput.WriteSerialized(path, writer)
            Else
                ' Also detects a closed controlled scope before any legacy backup/journal writes.
                If Not ControlledFileOutput.IsVerified(path) Then Throw New InvalidOperationException("Controlled ESP scope is closed.")
                BSA_BA2_Library_DLL.EscrituraEnElLugar.GuardarConCopia(path, writer)
            End If
        End Sub
    End Module
End Namespace
