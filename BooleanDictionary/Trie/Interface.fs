module CLI

open System
open System
open System
open System.Collections.Generic
open System.Collections.Generic
open System.Collections.Generic
open FParsec
open Dictionary.BiTrie
open System.IO
open System.Text.RegularExpressions
open Dictionary.Parser

let private DictFromFiles (folderPath: string) =
        let files = Directory.GetFiles(folderPath)
        let dict = BiTrieDictionary(Array.map (fun f -> FileInfo(f).Name) files)
        for i in 0..files.Length-1 do
            let file = files.[i]
            use sr = new StreamReader(file)
            while not sr.EndOfStream do
                let line = sr.ReadLine().Split([|" "; "--"; ";"; "."; ","; "!"; "?"; "*"; ":"; "("; ")"; "["; "]";|],
                                    StringSplitOptions.RemoveEmptyEntries)
                Array.map (fun (s:string) ->
                    s.Trim('—', '’', '-', '“', '”', '"', '\'', '°', '«', '‹', '»', '›', '„', '‟', '〝', '_')) line
                |> Array.filter (fun s -> Regex.IsMatch(s, "\\w+"))
                |> Array.map (fun s -> s.ToLowerInvariant())
                |> Array.iter (fun s -> dict.AddWord (s, i))
        dict

let GetQuery (trie : BiTrie) parserResult =
    match parserResult with
    | Success (value, _, _) ->
        match value with
        | StartOfWord s -> trie.StartWith s
        | EndOfWord e -> trie.EndWith e
        | MiddleOfWord (s,e) -> trie.StartEndWith s e 
        | WithoutJoker w -> if trie.ContainsWord w then seq {w}
                            else printfn "Word has not been found"
                                 Seq.empty
    | Failure _ -> printfn "Incorrect query"
                   Seq.empty
    
    

[<EntryPoint>]
let CLI _ =
    let Dictionary = DictFromFiles @"C:\Users\Юрий\Documents\Книги"
    printfn "Dictionary size: %i" Dictionary.Size
    while true do
        printf "Enter query: "
        let query = System.Console.ReadLine().ToLowerInvariant()
        let stopWatch = System.Diagnostics.Stopwatch.StartNew()
        let list = ParseString query |> GetQuery Dictionary |> Seq.toList
        stopWatch.Stop()
        List.iter (fun word -> printfn "%s" word) list
        printfn "Word count: %i   Time elapsed: %f milliseconds" list.Length stopWatch.Elapsed.TotalMilliseconds
        printfn "Found in documents: "
        Dictionary.GetDocs list |> Seq.iter (fun word -> printfn "%s" word)
        printfn ""
    0
