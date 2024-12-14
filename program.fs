module program.fs

open System
open System.Windows.Forms
open System.Text.Json
open System.IO

type Book = {
    title: string
    author: string
    genre: string
    isBorrowed: bool
    borrowDate: DateTime option
}

// Function to display all books
let displayBooks (filePath: string) (listView: ListView) =
    // Clear the current items in the ListView
    listView.Items.Clear()
    
    if File.Exists(filePath) then
        let jsonString = File.ReadAllText(filePath)
        let options = JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
        
        try
            let books = JsonSerializer.Deserialize<Book list>(jsonString, options)
            books |> List.iter (fun book -> 
                let status = if book.isBorrowed then "Borrowed" else "Available"
                let item = new ListViewItem([| book.title; book.author; book.genre; status |])
                listView.Items.Add(item) |> ignore
            )
        with
        | ex -> 
            MessageBox.Show(sprintf "Error deserializing JSON: %s" ex.Message) |> ignore
    else
        MessageBox.Show(sprintf "File %s does not exist." filePath) |> ignore

// Function to add a new book
let addBook (newBook: Book) (filePath: string) (listView: ListView) =
    if String.IsNullOrWhiteSpace(newBook.title) then
        MessageBox.Show("Book title cannot be empty or whitespace.") |> ignore
    else
        let options = JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true)
        let books = 
            if File.Exists(filePath) then
                try JsonSerializer.Deserialize<Book list>(File.ReadAllText(filePath), options)
                with _ -> []
            else []

        // Check for duplicates
        if books |> List.exists (fun book -> book.title = newBook.title) then
            MessageBox.Show(sprintf "The book '%s' already exists." newBook.title) |> ignore
        else
            let updatedBooks = newBook :: books
            let updatedJson = JsonSerializer.Serialize(updatedBooks, options)
            File.WriteAllText(filePath, updatedJson)
            // Automatically refresh the book display
            displayBooks filePath listView
            MessageBox.Show(sprintf "Book '%s' added successfully." newBook.title) |> ignore

// Function to search for a book
let searchBook (title: string) (filePath: string) =
    if String.IsNullOrWhiteSpace(title) then
        MessageBox.Show("Please enter a valid book title to search.") |> ignore
    elif File.Exists(filePath) then
        let jsonString = File.ReadAllText(filePath)
        let options = JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
        try
            let books = JsonSerializer.Deserialize<Book list>(jsonString, options)
            let foundBooks = books |> List.filter (fun book -> book.title.Contains(title, StringComparison.OrdinalIgnoreCase))
            if foundBooks.IsEmpty then
                MessageBox.Show(sprintf "No books found with the title '%s'" title) |> ignore
            else
                foundBooks |> List.iter (fun book -> 
                    let status = if book.isBorrowed then "Borrowed" else "Available"
                    MessageBox.Show(sprintf "Title: %s, Author: %s, Genre: %s, Status: %s" book.title book.author book.genre status) |> ignore
                )
        with
        | ex -> 
            MessageBox.Show(sprintf "Error deserializing JSON: %s" ex.Message) |> ignore
    else
        MessageBox.Show(sprintf "File %s does not exist." filePath) |> ignore

// Function to borrow book        
let borrowBook (title: string) (filePath: string) (listView: ListView) =
    if String.IsNullOrWhiteSpace(title) then
        MessageBox.Show("Please enter a valid book title to borrow.") |> ignore
    elif File.Exists(filePath) then
        let jsonString = File.ReadAllText(filePath)
        let options = JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true)
        try
            let books = JsonSerializer.Deserialize<Book list>(jsonString, options)
            let bookFound = ref false

            let updatedBooks = 
                books |> List.map (fun book -> 
                    if book.title = title then
                        bookFound := true
                        if book.isBorrowed then
                            MessageBox.Show(sprintf "Book '%s' is already borrowed." title) |> ignore
                            book
                        else
                            { book with isBorrowed = true; borrowDate = Some DateTime.Now }
                    else book
                )

            if not !bookFound then
                MessageBox.Show(sprintf "Book '%s' not found." title) |> ignore
            else
                let updatedJson = JsonSerializer.Serialize(updatedBooks, options)
                File.WriteAllText(filePath, updatedJson)
                // Automatically refresh the book display
                displayBooks filePath listView
                MessageBox.Show(sprintf "Book '%s' borrowed successfully on %s." title (DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))) |> ignore
        with
        | ex -> MessageBox.Show(sprintf "Error: %s" ex.Message) |> ignore
    else
        MessageBox.Show(sprintf "File %s does not exist." filePath) |> ignore

// Return a Borrowed Book
let returnBorrowedBook (title: string) (filePath: string) (listView: ListView) =
    if String.IsNullOrWhiteSpace(title) then
        MessageBox.Show("Please enter a valid book title to return.") |> ignore
    elif File.Exists(filePath) then
        let jsonString = File.ReadAllText(filePath)
        let options = JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true)
        try
            let books = JsonSerializer.Deserialize<Book list>(jsonString, options)
            let bookFound = ref false

            let updatedBooks = 
                books |> List.map (fun book -> 
                    if book.title = title then
                        bookFound := true
                        if book.isBorrowed then
                            { book with isBorrowed = false; borrowDate = None }
                        else
                            MessageBox.Show(sprintf "Book '%s' is not currently borrowed." title) |> ignore
                            book
                    else book
                )

            if not !bookFound then
                MessageBox.Show(sprintf "Book '%s' not found." title) |> ignore
            else
                let updatedJson = JsonSerializer.Serialize(updatedBooks, options)
                File.WriteAllText(filePath, updatedJson)
                // Automatically refresh the book display
                displayBooks filePath listView
                MessageBox.Show(sprintf "Book '%s' returned successfully." title) |> ignore
        with
        | ex -> MessageBox.Show(sprintf "Error: %s" ex.Message) |> ignore
    else
        MessageBox.Show(sprintf "File %s does not exist." filePath) |> ignore

// UI Code
let form = new Form(Text = "Library Management System", Width = 800, Height = 600)

let titleLabel = new Label(Text = "Title:", Top = 20, Left = 20)
let titleTextBox = new TextBox(Top = 20, Left = 150, Width = 200)

let authorLabel = new Label(Text = "Author:", Top = 60, Left = 20)
let authorTextBox = new TextBox(Top = 60, Left = 150, Width = 200)

let genreLabel = new Label(Text = "Genre:", Top = 100, Left = 20)
let genreTextBox = new TextBox(Top = 100, Left = 150, Width = 200)

let addButton = new Button(Text = "Add", Top = 140, Left = 100)
let searchButton = new Button(Text = "Search", Top = 180, Left = 100)
let borrowButton = new Button(Text = "Borrow", Top = 220, Left = 100)
let returnButton = new Button(Text = "Return", Top = 260, Left = 100)

let displayLabel = new Label(Text = "Books Display:", Top = 300, Left = 20)
let booksListView = new ListView(Top = 320, Left = 20, Width = 740, Height = 200)
booksListView.View <- View.Details
booksListView.Columns.Add("Title", 180) |> ignore
booksListView.Columns.Add("Author", 180) |> ignore
booksListView.Columns.Add("Genre", 180) |> ignore
booksListView.Columns.Add("Status", 180) |> ignore

let filePath = "books.json"

// Event Handlers
addButton.Click.Add(fun _ -> 
    let newBook = {
        title = titleTextBox.Text
        author = authorTextBox.Text
        genre = genreTextBox.Text
        isBorrowed = false
        borrowDate = None
    }
    addBook newBook filePath booksListView
)

searchButton.Click.Add(fun _ -> 
    let title = titleTextBox.Text
    searchBook title filePath
)

borrowButton.Click.Add(fun _ -> 
    let title = titleTextBox.Text
    borrowBook title filePath booksListView
)

returnButton.Click.Add(fun _ -> 
    let title = titleTextBox.Text
    returnBorrowedBook title filePath booksListView
)

displayBooks filePath booksListView

// Add controls to form
form.Controls.AddRange([| titleLabel; titleTextBox; authorLabel; authorTextBox;
                          genreLabel; genreTextBox; addButton; searchButton;
                          borrowButton; returnButton; displayLabel; booksListView |])

[<STAThread>]
Application.Run(form)
