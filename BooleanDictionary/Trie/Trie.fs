module Dictionary.Trie

open System
open System.Collections.Generic


type internal Node =
    | Value of children: SortedDictionary<char, Node>
    | EndOfWord

let rec private CreateNode children =
    let dictionary = SortedDictionary<char, Node>()
    match children with
    | [] -> dictionary.Add(Char.MinValue, EndOfWord)
    | x :: xs -> dictionary.Add(x, CreateNode xs)
    Value dictionary

let rec private AppendWord node children =
    match node with
    | Value dictionary ->
        match children with
        | [] -> EndOfWord
        | x :: xs ->
            if dictionary.ContainsKey(x) then AppendWord (dictionary.[x]) xs |> ignore
            else dictionary.Add(x, CreateNode xs)
            node
    | EndOfWord -> failwith "Incorrect node was passed as an argument"

let rec private Words node =
    seq {
        match node with
        | EndOfWord -> yield List.empty
        | Value children ->
            for keyvalue in children do
                for word in (Words keyvalue.Value) do
                    match keyvalue.Key with
                    | Char.MinValue -> yield word
                    | other -> yield (string other) :: word
    }



let rec private traverseTrie (node: Node) word =
    match node with
    | EndOfWord -> failwith "Incorrectly built tree"
    | Value dictionary ->
        match word with
        | [] -> node
        | x :: xs ->
            if (dictionary.ContainsKey(x)) then traverseTrie dictionary.[x] xs
            else EndOfWord


let private containsWord (node: Node) word =
    match (traverseTrie node word) with
    | Value dictionary -> dictionary.ContainsKey(Char.MinValue)
    | _ -> false

let rec private toDelete (current: Node) candidate word =
    match current with
    | EndOfWord -> failwith "Incorrectly built tree"
    | Value dictionary ->
        match word with
        | [] ->
            if (dictionary.Count > 1) then (current, Char.MinValue)
            else candidate
        | x :: xs ->
            if (not (dictionary.ContainsKey(x))) then (EndOfWord, '') //Word isn't in dictionary
            elif (dictionary.Count > 1) then toDelete dictionary.[x] (current, x) xs
            else toDelete dictionary.[x] candidate xs


let rec private Count node =
    match node with
    | EndOfWord -> 1
    | Value dictionary ->
        dictionary.Values |> Seq.sumBy Count
        
type ForwardTrie() =

    member val internal Root = Value (SortedDictionary<char, Node>())

    member this.AddWord string =
        string
        |> Seq.toList
        |> function
        | [] -> ()
        | x -> AppendWord this.Root x |> ignore
    member this.Size = Count this.Root
    member this.ContainsWord word = containsWord this.Root (Seq.toList word)

    member this.Words query =
        let traversal = (traverseTrie this.Root <| Seq.toList query)
        match traversal with
        | EndOfWord -> Seq.empty
        | Value _ ->
            Words traversal
            |> Seq.map (fun list ->
                let word = list |> String.concat ""
                query + word)

    member this.DeleteWord word =
        let charList = Seq.toList word
        match charList, this.Root with
        | ([], _) -> ()
        | (x :: _, Value dictionary) ->
            if dictionary.ContainsKey(x) then
                let candidate = toDelete this.Root (this.Root, x) charList
                match candidate with
                | (Value dictionary, letter) -> (dictionary.Remove(letter)) |> ignore
                | (EndOfWord, _) -> printfn "Word isn't present in dictionary" |> ignore
        | _ -> failwith "Incorrectly built tree"


type ReverseTrie() =
    inherit ForwardTrie()
    member this.Words query =
        let traversal = (traverseTrie this.Root (Seq.toList query |> List.rev))
        match traversal with
        | EndOfWord -> Seq.empty
        | Value _ ->
            Words traversal
            |> Seq.map (fun list ->
                let word = List.rev list |> String.concat ""
                word + query)

    member this.DeleteWord word =
        let charList = Seq.toList word |> List.rev
        match charList, this.Root with
        | ([], _) -> ()
        | (x :: _, Value dictionary) ->
            if dictionary.ContainsKey(x) then
                let candidate = toDelete this.Root (this.Root, x) charList
                match candidate with
                | (Value dictionary, letter) -> (dictionary.Remove(letter)) |> ignore
                | (EndOfWord, _) -> printfn "Word isn't present in dictionary" |> ignore
        | _ -> failwith "Incorrectly built tree"

    member this.ContainsWord word = containsWord this.Root (Seq.toList word |> List.rev)
    member this.AddWord string =
        string
        |> Seq.toList
        |> List.rev
        |> function
        | [] -> ()
        | x -> AppendWord this.Root x |> ignore
