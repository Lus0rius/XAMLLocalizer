Imports System.IO
Imports System.Environment
Imports System.Xml
Imports System.Xml.Linq
Imports System.Collections.Generic
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Linq
Imports System.Net
Imports System.Reflection
Imports System.Security.AccessControl
Imports <xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
Imports <xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
Imports <xmlns:system="clr-namespace:System;assembly=mscorlib">
Imports XAMLLocalizer.MainWindow
Imports System.Windows.Threading
Imports System.Formats.Asn1.AsnWriter

Class MainWindow
    Dim mainfile As String = ""
    Public Shared appdata As String = GetFolderPath(SpecialFolder.ApplicationData)
    Public Shared settings As String = appdata & "\Lus0rius Tools\LocalizeSettings.ini"
    Dim currentbox As TextBox
    Dim xmlitems As List(Of Xitem) = New List(Of Xitem)
    Dim showspaces As String = "false"
    Dim deletedItems As List(Of Tuple(Of Integer, Xitem)) = New List(Of Tuple(Of Integer, Xitem))

    Public Shared xmlwsettings = New XmlWriterSettings With {
            .Indent = True,
            .IndentChars = vbTab,
            .Encoding = New UTF8Encoding(False),
            .OmitXmlDeclaration = True
        }

    Dim xns As XNamespace = "http://schemas.microsoft.com/winfx/2006/xaml"
    Dim sns As XNamespace = "clr-namespace:System;assembly=mscorlib"

    Public Sub New()

        ' Cet appel est requis par le concepteur.
        InitializeComponent()

        BindingOperations.EnableCollectionSynchronization(xmlitems, XMLList)

        ' Ajoutez une initialisation quelconque après l'appel InitializeComponent().

    End Sub


    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If Not System.IO.Directory.Exists(appdata & "\Lus0rius Tools") Then
            System.IO.Directory.CreateDirectory(appdata & "\Lus0rius Tools")
        End If

        If Not System.IO.File.Exists(settings) Then
            File.Create(settings).Dispose()
        End If


        If Not File.ReadAllText(settings).Contains("currentdoc = ") Then
            Using sw As New StreamWriter(settings, True)
                sw.WriteLine("currentdoc = ")
            End Using
        End If

        If Not File.ReadAllText(settings).Contains("showspaces = ") Then
            Using sw As New StreamWriter(settings, True)
                sw.WriteLine("showspaces = ")
            End Using
        End If

        'Detection du chemin du document actuel
        mainfile = ReadSettings("currentdoc")
        showspaces = ReadSettings("showspaces")
        If showspaces = "true" Then
            XMLList.FontFamily = New FontFamily("Courier New Space Bold")
            SpacesButton.Header = "Hide spaces"
        End If

        If File.Exists(mainfile) Then
            Reload(sender, e)
        Else
            OpenButton_Click(sender, e)
        End If
    End Sub

    Private Sub ReloadButton_Click(sender As Object, e As RoutedEventArgs) Handles ReloadButton.Click
        Reload(sender, e)
    End Sub

    Private Sub AddLocalization(elmntName As String, elmntValue As String, lang As String)
        Select Case lang
            Case "fr"
                xmlitems.Add(New Xitem With {
                             .Element = elmntName,
                             .French = elmntValue})
            Case "de"
                xmlitems.Add(New Xitem With {
                             .Element = elmntName,
                             .German = elmntValue})
            Case Else
                xmlitems.Add(New Xitem With {
                             .Element = elmntName,
                             .English = elmntValue})
        End Select
    End Sub

    Private Sub UpdateLocalization(elmntValue As String, lang As String, index As Integer)
        If elmntValue = xmlitems(index).English Then Exit Sub

        Select Case lang
            Case "fr"
                xmlitems(index).French = elmntValue
            Case "de"
                xmlitems(index).German = elmntValue
            Case Else
                xmlitems(index).English = elmntValue
        End Select
    End Sub

    Private Sub Reload(sender As Object, e As RoutedEventArgs)
        xmlitems.Clear()



        Dim files = New List(Of String) From {mainfile}

        For Each entry In Directory.GetFiles(Path.GetDirectoryName(mainfile))
            If Path.GetFileNameWithoutExtension(entry).StartsWith($"{Path.GetFileNameWithoutExtension(mainfile)}.") AndAlso Path.GetExtension(entry).ToLower = ".xaml" Then
                files.Add(entry)
            End If
        Next

        For Each f In files
            Dim lang As String = Path.GetFileNameWithoutExtension(f).Split("."c).LastOrDefault
            Dim xdoc As XDocument = TryLoad(f)
            If xdoc Is Nothing Then Exit Sub

            For Each elmnt As XElement In xdoc.Root.Elements
                Dim elmntName = elmnt.Attribute(xns + "Key").Value.ToString
                Dim elmntValue = elmnt.Value.ToString

                Dim elementFound = xmlitems.Find(Function(c) c.Element = elmntName)
                If elementFound IsNot Nothing Then
                    UpdateLocalization(elmntValue, lang, xmlitems.IndexOf(elementFound))
                    Continue For
                End If

                AddLocalization(elmntName, elmntValue, lang)
            Next
        Next

        xmlitems.Add(New Xitem)

        XMLList.ItemsSource = xmlitems
        CollectionViewSource.GetDefaultView(xmlitems).Refresh()

        Me.Title = Path.GetFileName(mainfile)
        UpdateSettings("currentdoc", mainfile)

        If currentbox IsNot Nothing Then
            currentbox.Focus()
        End If
    End Sub

    Public Shared Function TryLoad(f As Object)
        Dim doc As XDocument

        Try
            doc = XDocument.Load(f)
        Catch
            If f <> settings And File.Exists(settings) Then
                Dim msg = MessageBox.Show(Path.GetFileName(f).ToString & " is an invalid xml file. Would you try to repair it ?", "Lus0rius Tools", vbYesNo)
                If msg = MessageBoxResult.Yes Then
                    Try
                        Dim str = Regex.Replace(File.ReadAllText(f), "\&(apos|#\d*)([^[\d;])", "&$1;$2")
                        File.WriteAllText(f, str.Replace(ChrW(&H0), ChrW(&H2400)).Replace("'", "&apos;"))
                    Catch
                        MessageBox.Show("[XML Handler] File " & Path.GetFileName(f) & " could not be accessed. Try restarting the software with administrator rights.")
                        Return Nothing
                    End Try
                End If
            End If
            Console.WriteLine(f)
            Console.WriteLine(settings)
            doc = XDocument.Load(f)
            Try
                doc = XDocument.Load(f)
            Catch
                MessageBox.Show("[XML Handler] File " & Path.GetFileName(f) & " could not be read.")
                Return Nothing
            End Try
        End Try

        Return doc
    End Function

    Private Sub TextBox_TextChanged()
        If Not Me.Title.EndsWith("*") Then
            Me.Title += "*"
        End If
    End Sub

    Private Sub TextBox_GotFocus(sender As Object, e As RoutedEventArgs)
        If TryCast(sender, TextBox) IsNot Nothing Then currentbox = TryCast(sender, TextBox)

        Dim blankItemsNb As Integer = (From i In xmlitems Where String.IsNullOrWhiteSpace(i.Element)
                                       Select i).Count
        blankItemsNb = If(blankItemsNb > 0, blankItemsNb - 1, blankItemsNb)
        Dim duplicatesNb As Integer = xmlitems.Count - blankItemsNb - (From i In xmlitems
                                                                       Select i.Element.ToLower
                                                                       Distinct).Count
        DuplicatesLabel.Content = duplicatesNb
        DuplicatesLabel.Background = If(duplicatesNb > 0, Brushes.LightCoral, Brushes.White)
    End Sub

    Private Sub Invisible(sender As Object, e As RoutedEventArgs)
        'Dim parent As GridViewColumn = CType(VisualTreeHelperExtensions.FindVisualParent(Of GridViewColumn)(sender), GridViewColumn)

        'currentbox = TryCast(sender, TextBox)
        'Dim cp As ContentPresenter = currentbox.TemplatedParent
        'Dim rowpres As GridViewRowPresenter = cp.Parent
        'Dim columns = rowpres.Columns

        Dim blankLine As Xitem = xmlitems(xmlitems.Count - 1)

        For Each p In blankLine.GetType.GetProperties
            Dim pVal = p.GetValue(blankLine)
            If Not String.IsNullOrEmpty(pVal) Then
                SyncLock XMLList
                    xmlitems.Add(New Xitem)
                End SyncLock
                CollectionViewSource.GetDefaultView(xmlitems).Refresh()

                'Dim previousItem = XMLList.Items(XMLList.Items.Count - 2)

                'Dim generator = XMLList.ItemContainerGenerator
                'Dim container As ListViewItem = CType(generator.ContainerFromItem(previousItem), ListViewItem)
                'Dim dependencyObject = CType(VisualTreeHelperExtensions.FindVisualChild(Of TextBox)(container), TextBox)

                Keyboard.ClearFocus()
                currentbox = FindVisualParent(Of TextBox)(Mouse.DirectlyOver)
                Dispatcher.BeginInvoke(DispatcherPriority.Render, New Action(Sub()
                                                                                 currentbox.Focus()         ' Set Logical Focus
                                                                                 Keyboard.Focus(currentbox) ' Set Keyboard Focus
                                                                             End Sub))
                Dim fe = FocusManager.GetFocusedElement(Me)
                Exit Sub
            End If
        Next

    End Sub

    Private Sub SaveButton_Click(sender As Object, e As RoutedEventArgs) Handles SaveButton.Click
        If String.IsNullOrEmpty(mainfile) Then
            SaveAsButton_Click(sender, e)
        End If

        Saving(mainfile)
    End Sub

    Private Sub SaveAsButton_Click(sender As Object, e As RoutedEventArgs) Handles SaveAsButton.Click
        Dim dialog As New Microsoft.Win32.SaveFileDialog() With {
            .Title = "Save as...",
            .InitialDirectory = If(mainfile Is Nothing, System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Path.GetDirectoryName(mainfile)),
            .Filter = "XAML Resource dictionary|*.xaml"
        }

        If dialog.ShowDialog = True And dialog.FileName IsNot Nothing Then
            Dim file = dialog.FileName
            Saving(file)
        End If
    End Sub

    Private Function GetElementFromLanguage(elmnt As Xitem, lang As String)
        If elmnt.English Is Nothing And elmnt.French Is Nothing And elmnt.German Is Nothing Then Return Nothing

        Dim localization As String = elmnt.English

        Select Case lang
            Case "fr"
                Dim loc = elmnt.French
                If Not String.IsNullOrEmpty(loc) Then
                    localization = loc
                End If
            Case "de"
                Dim loc = elmnt.German
                If Not String.IsNullOrEmpty(loc) Then
                    localization = loc
                End If
        End Select

        Dim elmntName As String = elmnt.Element
        If elmntName Is Nothing Then elmntName = String.Empty
        If localization Is Nothing Then localization = String.Empty
        Return New XElement(sns + "String", localization, New XAttribute(xns + "Key", elmntName))
    End Function

    Private Sub Saving(file As String)
        InvisibleTextBox.Clear()
        InvisibleTextBox.Focus()

        Dim langList = New List(Of String) From {String.Empty, "fr", "de"}

        For Each lang In langList
            Dim root = <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                           xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                           xmlns:system="clr-namespace:System;assembly=mscorlib"/>

            Dim ordered_xmlitems As List(Of Xitem) = xmlitems
            ordered_xmlitems = ordered_xmlitems.OrderBy(Function(bi) bi.Element).ToList()

            For Each elmnt As Xitem In ordered_xmlitems
                Dim newElement = GetElementFromLanguage(elmnt, lang)
                If newElement IsNot Nothing Then
                    root.Add(newElement)
                End If
            Next

            Dim doc As New XDocument
            doc = New XDocument(root)

            Dim f As String
            If lang = String.Empty Then
                f = $"{Path.GetDirectoryName(file)}\{Path.GetFileNameWithoutExtension(file)}.xaml"
            Else
                f = $"{Path.GetDirectoryName(file)}\{Path.GetFileNameWithoutExtension(file)}.{lang}.xaml"
            End If

            Using xw As XmlWriter = XmlWriter.Create(f, xmlwsettings)
                doc.Save(xw)
            End Using
        Next

        mainfile = file

        Me.Title = Path.GetFileName(mainfile)
        UpdateSettings("currentdoc", mainfile)

        If currentbox IsNot Nothing Then
            currentbox.Focus()
        End If
    End Sub

    Private Function ReadSettings(setting As String)
        Dim result As String = ""
        If System.IO.File.Exists(settings) Then
            Using sr As New StreamReader(settings)
                Dim line As String
                line = sr.ReadLine()

                Do While line IsNot Nothing
                    Select Case True
                        Case line.Contains(setting & " = ")
                            result = line.Replace(setting & " = ", "")
                    End Select
                    line = sr.ReadLine
                Loop
            End Using

            If result IsNot Nothing Then
                Return result
            Else
                Return False
            End If
        Else
            Return False
        End If
    End Function

    Private Sub UpdateSettings(setting As String, newset As String)
        Dim oldset As String = ReadSettings(setting)
        Dim readAllSettings As String = System.IO.File.ReadAllText(settings)
        Using sw As New StreamWriter(settings)
            sw.Write(readAllSettings.Replace(setting & " = " & oldset, setting & " = " & newset))
        End Using
    End Sub

    Private Sub MainWindow_Closing(sender As Object, e As EventArgs) Handles Me.Closing
        UpdateSettings("currentdoc", mainfile)
        UpdateSettings("showspaces", showspaces)
    End Sub

    Private Sub OpenButton_Click(sender As Object, e As RoutedEventArgs) Handles OpenButton.Click
        Dim openDial As New Microsoft.Win32.OpenFileDialog()
        openDial.Title = "Please select the file to open"

        Dim docdir = Path.GetDirectoryName(mainfile)

        If docdir <> "" And Directory.Exists(docdir) Then
            openDial.InitialDirectory = docdir
        Else
            openDial.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        End If

        openDial.Filter = "XAML Resource dictionary|*.xaml"

        If openDial.ShowDialog = True Then
            mainfile = openDial.FileName
            Reload(sender, e)
        End If

        If currentbox IsNot Nothing Then
            currentbox.Focus()
        End If
    End Sub

    Private Sub DeleteRow(sender As Button, e As RoutedEventArgs)
        Dim row As Xitem = sender.DataContext

        deletedItems.Add(New Tuple(Of Integer, Xitem)(xmlitems.IndexOf(row), row))
        xmlitems.Remove(row)
        CollectionViewSource.GetDefaultView(xmlitems).Refresh()

        TextBox_GotFocus(sender, e)
        TextBox_TextChanged()
    End Sub

    Private Sub SpacesButton_Click(sender As Object, e As RoutedEventArgs) Handles SpacesButton.Click
        InvisibleTextBox.Clear()
        InvisibleTextBox.Focus()

        If XMLList.FontFamily.ToString = "Segoe UI" Then
            XMLList.FontFamily = New FontFamily("Courier New Space Bold")
            SpacesButton.Header = "Hide spaces"
            showspaces = "true"
        Else
            XMLList.FontFamily = New FontFamily("Segoe UI")
            SpacesButton.Header = "Display spaces"
            showspaces = "false"
        End If
        CollectionViewSource.GetDefaultView(xmlitems).Refresh()

        If currentbox IsNot Nothing Then
            currentbox.Focus()
        End If
    End Sub

    Private Sub CountButton_Click(sender As Object, e As RoutedEventArgs) Handles CountButton.Click
        InvisibleTextBox.Clear()
        InvisibleTextBox.Focus()

        Dim stringen As String = ""
        Dim stringfr As String = ""
        Dim stringde As String = ""
        For Each item As Xitem In xmlitems
            stringen += item.English & " "
            stringfr += item.French & " "
            stringde += item.German & " "
        Next
        Console.WriteLine(stringen)

        Dim counten As Integer = Regex.Matches(stringen, "\S+").Count
        Dim countfr As Integer = Regex.Matches(stringfr, "\S+").Count
        Dim countde As Integer = Regex.Matches(stringde, "\S+").Count

        MessageBox.Show("English: " & counten & " words" & vbLf & "French: " & countfr & " words" & vbLf & "German: " & countde & " words", "XML Word Count")

        If currentbox IsNot Nothing Then
            currentbox.Focus()
        End If
    End Sub

    Friend Class Xitem
        Public Property Element As String = String.Empty
        Public Property English As String = String.Empty
        Public Property French As String = String.Empty
        Public Property German As String = String.Empty
    End Class

    Private Sub XMLList_KeyDown(sender As Object, e As KeyEventArgs) Handles XMLList.KeyDown
        If e.Key = Key.Z And (Keyboard.IsKeyDown(Key.LeftCtrl) Or Keyboard.IsKeyDown(Key.RightCtrl)) Then
            UndoRemove()
        End If
    End Sub

    Private Sub UndoRemove() Handles UndoButton.Click
        If Not deletedItems.Any() Then Exit Sub

        xmlitems.Insert(deletedItems.Last.Item1, deletedItems.Last.Item2)
        deletedItems.Remove(deletedItems.Last)
        CollectionViewSource.GetDefaultView(xmlitems).Refresh()

        TextBox_GotFocus(New Object, New RoutedEventArgs)
        TextBox_TextChanged()
    End Sub

    Private Function GetDefaultIndex()
        Dim index As Integer
        index = XMLList.SelectedIndex
        If Index = -1 Then
            Index = XMLList.Items.Count - 1
        End If

        Return index
    End Function

    Private Sub AddButton_Click(sender As Object, e As RoutedEventArgs) Handles AddButton.Click
        Dim index As Integer

        If currentbox Is Nothing Then
            index = GetDefaultIndex()
        Else
            Dim dataContext = currentbox.DataContext
            If dataContext.GetType() IsNot GetType(Xitem) Then
                index = GetDefaultIndex()
            Else
                index = xmlitems.IndexOf(dataContext)
            End If
        End If

        index += 1

        SyncLock XMLList
            xmlitems.Insert(index, New Xitem)
        End SyncLock
        CollectionViewSource.GetDefaultView(xmlitems).Refresh()

        If (VisualTreeHelper.GetChildrenCount(XMLList) > 0) Then
            XMLList.ScrollIntoView(xmlitems(index))
        End If

        TextBox_GotFocus(sender, e)
    End Sub

    Private Sub OpenDirButton_Click(sender As Object, e As RoutedEventArgs) Handles OpenDirButton.Click
        If mainfile IsNot Nothing AndAlso File.Exists(mainfile) Then
            Process.Start("explorer.exe", "/select,""" & mainfile & """")
        End If
    End Sub
End Class

Module VisualTreeHelperExtensions
    Function FindVisualParent(Of T As DependencyObject)(ByVal depObj As DependencyObject) As T
        Dim parent = VisualTreeHelper.GetParent(depObj)
        If parent Is Nothing OrElse TypeOf parent Is T Then Return CType(parent, T)
        Return FindVisualParent(Of T)(parent)
    End Function

    Function FindVisualChild(Of T As Visual)(ByVal depObj As DependencyObject) As T
        If depObj IsNot Nothing Then

            For i As Integer = 0 To VisualTreeHelper.GetChildrenCount(depObj) - 1
                Dim child As DependencyObject = VisualTreeHelper.GetChild(depObj, i)

                If child IsNot Nothing AndAlso TypeOf child Is T Then
                    Return CType(child, T)
                End If

                For Each childOfChild As T In FindVisualChildren(Of T)(child)
                    Return childOfChild
                Next
            Next
        End If

        Return Nothing
    End Function

    Function FindVisualChild(Of T As FrameworkElement)(ByVal depObj As DependencyObject, ByVal name As String) As T
        If depObj IsNot Nothing Then

            For i As Integer = 0 To VisualTreeHelper.GetChildrenCount(depObj) - 1
                Dim child As DependencyObject = VisualTreeHelper.GetChild(depObj, i)

                If child IsNot Nothing AndAlso TypeOf child Is T AndAlso (TryCast(child, T)).Name.Equals(name) Then
                    Return CType(child, T)
                End If

                For Each childOfChild As T In FindVisualChildren(Of T)(child)
                    If childOfChild.Name.Equals(name) Then Return childOfChild
                Next
            Next
        End If

        Return Nothing
    End Function

    Iterator Function FindVisualChildren(Of T As DependencyObject)(ByVal depObj As DependencyObject) As IEnumerable(Of T)
        If depObj Is Nothing Then Return

        For i As Integer = 0 To VisualTreeHelper.GetChildrenCount(depObj) - 1
            Dim child As DependencyObject = VisualTreeHelper.GetChild(depObj, i)

            If child IsNot Nothing AndAlso TypeOf child Is T Then
                Yield CType(child, T)
            End If

            For Each childOfChild As T In FindVisualChildren(Of T)(child)
                Yield childOfChild
            Next
        Next
    End Function
End Module
