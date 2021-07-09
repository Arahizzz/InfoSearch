module Dictionary.BiTrie

open BinaryDictionary
open Trie
open System
open System.Collections.Generic
open System.Collections.Generic

let private (=~) a b  = String.Compare(a, b, StringComparison.Ordinal) = 0
let private (<~) a b  = String.Compare(a, b, StringComparison.Ordinal) < 0
let rec intersectLists first second =
    seq {
        match (first, second) with
        | ([], _) | (_, []) -> ()
        | (one::first, two::second) when one =~ two -> yield one
                                                       yield! intersectLists first second
        | (one::first, two::second) when one <~ two -> yield! intersectLists first (two::second)
        | (first, _::second) -> yield! intersectLists first second
    }
    

type BiTrie() =
    let ForwardTrie = ForwardTrie()
    let ReverseTrie = ReverseTrie()
    
    member this.Size = ForwardTrie.Size
    member this.ContainsWord word = ForwardTrie.ContainsWord word
    member this.StartWith word = ForwardTrie.Words word
    member this.EndWith word = ReverseTrie.Words word |> Seq.sort 
    member this.StartEndWith start ending =
        let beginings = this.StartWith start
        let endings = this.EndWith ending
        intersectLists <| List.ofSeq beginings <| List.ofSeq endings
    member this.AddWord word =
        ForwardTrie.AddWord word
        ReverseTrie.AddWord word
    member this.DeleteWord word =
        ForwardTrie.DeleteWord word
        ReverseTrie.DeleteWord word
        
type BiTrieDictionary(documents) =
    inherit BiTrie()
    let docs = SetDictionary(documents)
    
    member this.AddWord (word, docID) =
        docs.AddWord(word, docID)
        base.AddWord word
        
    member this.GetDocs wordList = docs.VOr(wordList |> Array.ofList)