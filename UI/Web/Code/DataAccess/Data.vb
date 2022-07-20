Imports System.Data.SqlClient
Imports System.Data.Objects.DataClasses

Namespace Predictathon
    Public MustInherit Class Manager
    End Class

    Public Class Data

#Region " Properties "
        ''a thought - using one context per HTTP request. I'm not convinced by the logic of doing this,
        ''so it's on the back-burner for now.
        'Public Shared ReadOnly Property CurrentContext() As PredictathonEntities.PredictathonEntities
        '    Get
        '        If Not IsNothing(HttpContext.Current) Then
        '            Dim strKey As String = "objContext_" & HttpContext.Current.GetHashCode.ToString
        '            If Not HttpContext.Current.Items.Contains(strKey) Then
        '                HttpContext.Current.Items.Add(strKey, New PredictathonEntities.PredictathonEntities)
        '            End If
        '            Return DirectCast(HttpContext.Current.Items(strKey), PredictathonEntities.PredictathonEntities)
        '        Else
        '            Throw New System.Exception("No HTTP context available.")
        '        End If
        '    End Get
        'End Property
#End Region

        Protected Const ContainerName As String = "PredictathonEntities"
        Protected Const ConnectionStringName As String = "Predictathon"

        'call a stored procedure
        Public Shared Function CallSP(ByVal StoredProcedureName As String, ByVal Params As List(Of SqlParameter)) As DataTable
            Dim objDataTable As New DataTable

            Using objSQLConnection As New System.Data.SqlClient.SqlConnection(ConfigurationManager.ConnectionStrings(ConnectionStringName).ConnectionString)
                objSQLConnection.Open()
                Using objSQLCommand As New SqlCommand
                    With objSQLCommand
                        .CommandText = StoredProcedureName
                        .CommandType = CommandType.StoredProcedure
                        .Connection = objSQLConnection
                    End With

                    For Each objSQLParameter As SqlParameter In Params
                        objSQLCommand.Parameters.Add(objSQLParameter)
                    Next

                    ' Define the data adapter and fill the dataset 
                    Using objSQLDataAdaptor As New SqlDataAdapter(objSQLCommand)
                        objDataTable = New DataTable
                        objSQLDataAdaptor.Fill(objDataTable)
                    End Using
                End Using

                Return objDataTable
            End Using
        End Function

        ' Generic save method
        Public Shared Sub Save(Entity As EntityObject, EntitySetName As String)
            Using objContext As New PredictathonModel.PredictathonEntities
                ' Does the object already exist in the context? If not, we need to add it
                Dim blnIsNew As Boolean = False
                Dim objStateEntry As System.Data.Objects.ObjectStateEntry = Nothing

                If IsNothing(Entity.EntityKey) Then
                    objContext.AttachTo(EntitySetName, Entity)
                    blnIsNew = True
                End If

                If objContext.ObjectStateManager.TryGetObjectStateEntry(Entity.EntityKey, objStateEntry) = False Then
                    objContext.Attach(Entity)
                End If

                ' Apply changes and save the entity
                objContext.ApplyCurrentValues(EntitySetName, Entity)

                If blnIsNew Then
                    objContext.ObjectStateManager.ChangeObjectState(Entity, EntityState.Added)
                Else
                    objContext.ObjectStateManager.ChangeObjectState(Entity, EntityState.Modified)
                End If

                objContext.SaveChanges()
            End Using
        End Sub

        'generic delete method
        Public Shared Sub Delete(ByVal Entity As EntityObject, ByVal EntitySetName As String)
            Using objContext As New PredictathonModel.PredictathonEntities
                'does the object already exist in the context? If not, we need to add it
                Dim objStateEntry As System.Data.Objects.ObjectStateEntry = Nothing
                If objContext.ObjectStateManager.TryGetObjectStateEntry(Entity.EntityKey, objStateEntry) = False Then
                    objContext.Attach(Entity)
                End If
                'delete the entity
                objContext.ObjectStateManager.ChangeObjectState(Entity, EntityState.Deleted)
                objContext.SaveChanges()
            End Using
        End Sub
    End Class

    'Public Interface IRepository(Of T As Class)
    '    Inherits IDisposable
    '    Function Fetch() As IQueryable(Of T)
    '    Function GetAll() As IEnumerable(Of T)
    '    Function Find(ByVal predicate As Func(Of T, Boolean)) As IEnumerable(Of T)
    '    Function [Single](ByVal predicate As Func(Of T, Boolean)) As T
    '    Function First(ByVal predicate As Func(Of T, Boolean)) As T
    '    Sub Add(ByVal entity As T)
    '    Sub Delete(ByVal entity As T)
    '    Sub Attach(ByVal entity As T)
    '    Sub SaveChanges()
    '    Sub SaveChanges(ByVal options As SaveOptions)
    'End Interface

    ' ''' <summary>
    ' ''' A generic repository for working with data in the database
    ' ''' </summary>
    ' ''' <typeparam name="T">An object that represents an Entity Framework entity</typeparam>
    'Public Class DataRepository(Of T As Class)
    '    Implements IRepository(Of T)
    '    ''' <summary>
    '    ''' The context object for the database
    '    ''' </summary>
    '    Private _context As ObjectContext
    '    ''' <summary>
    '    ''' The IObjectSet that represents the current entity.
    '    ''' </summary>
    '    Private _objectSet As IObjectSet(Of T)
    '    ''' <summary>
    '    ''' Initializes a new instance of the DataRepository class
    '    ''' </summary>
    '    Public Sub New()
    '        Me.New(New PredictathonEntities.PredictathonEntities())
    '    End Sub

    '    ''' <summary>
    '    ''' Initializes a new instance of the DataRepository class
    '    ''' </summary>
    '    ''' <param name="context">The Entity Framework ObjectContext</param>
    '    Public Sub New(ByVal context As ObjectContext)
    '        _context = context
    '        _objectSet = _context.CreateObjectSet(Of T)()
    '    End Sub

    '    ''' <summary>
    '    ''' Gets all records as an IQueryable
    '    ''' </summary>
    '    ''' <returns>An IQueryable object containing the results of the query</returns>
    '    Public Function Fetch() As IQueryable(Of T) Implements IRepository(Of T).Fetch
    '        Return _objectSet
    '    End Function

    '    ''' <summary>
    '    ''' Gets all records as an IEnumberable
    '    ''' </summary>
    '    ''' <returns>An IEnumberable object containing the results of the query</returns>
    '    Public Function GetAll() As IEnumerable(Of T) Implements IRepository(Of T).GetAll
    '        Return Fetch().AsEnumerable()
    '    End Function

    '    ''' <summary>
    '    ''' Finds a record with the specified criteria
    '    ''' </summary>
    '    ''' <param name="predicate">Criteria to match on</param>
    '    ''' <returns>A collection containing the results of the query</returns>
    '    Public Function Find(ByVal predicate As Func(Of T, Boolean)) As IEnumerable(Of T) Implements IRepository(Of T).Find
    '        Return _objectSet.Where(predicate)
    '    End Function

    '    ''' <summary>
    '    ''' Gets a single record by the specified criteria (usually the unique identifier)
    '    ''' </summary>
    '    ''' <param name="predicate">Criteria to match on</param>
    '    ''' <returns>A single record that matches the specified criteria</returns>
    '    Public Function [Single](ByVal predicate As Func(Of T, Boolean)) As T Implements IRepository(Of T).Single
    '        Return _objectSet.[Single](predicate)
    '    End Function

    '    ''' <summary>
    '    ''' The first record matching the specified criteria
    '    ''' </summary>
    '    ''' <param name="predicate">Criteria to match on</param>
    '    ''' <returns>A single record containing the first record matching the specified criteria</returns>
    '    Public Function First(ByVal predicate As Func(Of T, Boolean)) As T Implements IRepository(Of T).First
    '        Return _objectSet.First(predicate)
    '    End Function

    '    ''' <summary>
    '    ''' Deletes the specified entity
    '    ''' </summary>
    '    ''' <param name="entity">Entity to delete</param>
    '    ''' <exception cref="ArgumentNullException"> if <paramref name="entity"/> is null</exception>
    '    Public Sub Delete(ByVal entity As T) Implements IRepository(Of T).Delete
    '        If entity Is Nothing Then
    '            Throw New ArgumentNullException("entity")
    '        End If
    '        _objectSet.DeleteObject(entity)
    '    End Sub

    '    ''' <summary>
    '    ''' Deletes records matching the specified criteria
    '    ''' </summary>
    '    ''' <param name="predicate">Criteria to match on</param>
    '    Public Sub Delete(ByVal predicate As Func(Of T, Boolean))
    '        Dim records As IEnumerable(Of T) = From x In _objectSet.Where(predicate)
    '        For Each record As T In records
    '            _objectSet.DeleteObject(record)
    '        Next
    '    End Sub

    '    ''' <summary>
    '    ''' Adds the specified entity
    '    ''' </summary>
    '    ''' <param name="entity">Entity to add</param>
    '    ''' <exception cref="ArgumentNullException"> if <paramref name="entity"/> is null</exception>
    '    Public Sub Add(ByVal entity As T) Implements IRepository(Of T).Add
    '        If entity Is Nothing Then
    '            Throw New ArgumentNullException("entity")
    '        End If
    '        _objectSet.AddObject(entity)
    '    End Sub

    '    ''' <summary>
    '    ''' Attaches the specified entity
    '    ''' </summary>
    '    ''' <param name="entity">Entity to attach</param>
    '    Public Sub Attach(ByVal entity As T) Implements IRepository(Of T).Attach
    '        _objectSet.Attach(entity)
    '    End Sub

    '    ''' <summary>
    '    ''' Saves all context changes
    '    ''' </summary>
    '    Public Sub SaveChanges() Implements IRepository(Of T).SaveChanges
    '        _context.SaveChanges()
    '    End Sub

    '    ''' <summary>
    '    ''' Saves all context changes with the specified SaveOptions
    '    ''' </summary>
    '    ''' <param name="options">Options for saving the context</param>
    '    Public Sub SaveChanges(ByVal options As SaveOptions) Implements IRepository(Of T).SaveChanges
    '        _context.SaveChanges(options)
    '    End Sub

    '    ''' <summary>
    '    ''' Releases all resources used by the ObjectContext
    '    ''' </summary>
    '    Public Sub Dispose() Implements IDisposable.Dispose
    '        Dispose(True)
    '        GC.SuppressFinalize(Me)
    '    End Sub

    '    ''' <summary>
    '    ''' Releases all resources used by the ObjectContext
    '    ''' </summary>
    '    ''' <param name="disposing">A boolean value indicating whether or not to dispose managed resources</param>
    '    Protected Overridable Sub Dispose(ByVal disposing As Boolean)
    '        If disposing Then
    '            If _context IsNot Nothing Then
    '                _context.Dispose()
    '                _context = Nothing
    '            End If
    '        End If
    '    End Sub
    'End Class
End Namespace