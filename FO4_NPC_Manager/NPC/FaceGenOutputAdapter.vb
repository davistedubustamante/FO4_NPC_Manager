Option Strict On
Imports System.IO
Imports FO4_Base_Library

Namespace ExperimentalTransformation
    ' Dedicated headless diagnostic scope, not permission to start a global executor.
    ' NIF serialization stays in the original library; DDS encoding remains unchanged.
    Public Module FaceGenOutputAdapter
        Private active As ReservedWriteBudget
        Private controlledMode As Boolean
        Private ReadOnly outputGate As New Object
        Public Function BeginControlledOutput(budget As ReservedWriteBudget) As IDisposable
            ArgumentNullException.ThrowIfNull(budget)
            SyncLock outputGate
                If active IsNot Nothing Then Throw New InvalidOperationException("A controlled FaceGen output scope is already active.")
                active = budget : controlledMode = True
                Return New OutputScope(budget)
            End SyncLock
        End Function
        Private NotInheritable Class OutputScope
            Implements IDisposable
            Private budget As ReservedWriteBudget
            Public Sub New(value As ReservedWriteBudget)
                budget = value
            End Sub
            Public Sub Dispose() Implements IDisposable.Dispose
                SyncLock outputGate
                    If budget Is Nothing Then Return
                    If active Is budget Then active = Nothing
                    budget = Nothing
                End SyncLock
            End Sub
        End Class
        Public Sub WriteBytes(destination As String, bytes As Byte())
            SyncLock outputGate
                Dim budget = active
                If budget Is Nothing AndAlso controlledMode Then Throw New InvalidOperationException("Controlled FaceGen output is closed; no unbudgeted fallback.")
                If budget Is Nothing Then
                    BSA_BA2_Library_DLL.EscrituraEnElLugar.Escribir(destination, Sub(output) output.Write(bytes))
                    Return
                End If
                budget.WriteNew(RelativeDestination(budget, destination), bytes)
            End SyncLock
        End Sub
        Public Sub WriteNif(destination As String, nif As Nifcontent_Class_Manolo)
            SyncLock outputGate
                ArgumentNullException.ThrowIfNull(nif)
                Dim budget = active
                If budget Is Nothing AndAlso controlledMode Then Throw New InvalidOperationException("Controlled FaceGen output is closed; no unbudgeted fallback.")
                If budget Is Nothing Then
                    nif.Save_As_Manolo(destination, Overwrite:=True)
                    Return
                End If
                Dim relative = RelativeDestination(budget, destination)
                Dim bytes = nif.Save_To_Bytes_Manolo()
                budget.WriteNew(relative, bytes)
            End SyncLock
        End Sub
        Public Sub EnsureDirectory(destination As String)
            SyncLock outputGate
                If active Is Nothing Then
                    If controlledMode Then Throw New InvalidOperationException("Controlled output scope is closed.")
                    Directory.CreateDirectory(destination)
                Else
                    ' Defer creation to the gateway, after the complete payload reservation.
                    RelativeDestination(active, destination)
                End If
            End SyncLock
        End Sub
        Public Sub RejectUncontrolledDiagnostic()
            SyncLock outputGate
                If controlledMode Then Throw New InvalidOperationException("TGA/debug file writer is not admitted to controlled FaceGen output.")
            End SyncLock
        End Sub
        Private Function RelativeDestination(budget As ReservedWriteBudget, destination As String) As String
            Dim relative = Path.GetRelativePath(budget.DirectoryPath, Path.GetFullPath(destination))
            If Path.IsPathRooted(relative) OrElse relative = ".." OrElse relative.StartsWith(".." & Path.DirectorySeparatorChar, StringComparison.Ordinal) Then Throw New IOException("FaceGen output must stay within its controlled workspace.")
            Return relative.Replace(Path.DirectorySeparatorChar, "/"c)
        End Function
    End Module
End Namespace
