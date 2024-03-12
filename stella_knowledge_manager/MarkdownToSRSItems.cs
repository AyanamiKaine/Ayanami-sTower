using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stella_knowledge_manager
{
    /// <summary>
    /// The main idea of this class is the following:
    /// In Obsidian there are various formatting ways to define a cloze, a flashcard. https://github.com/st3v3nmw/obsidian-spaced-repetition
    /// 
    /// We should parse a note with various clozes, and flashcards and turn them into SRS items we can learn
    /// 
    /// One approach could be the following:
    /// 1. Parsing the note and turning it into various markdown files where each file only has one cloze and one flashcard(question and answer)
    /// 2. Save the Question/deleting the cloze and saving the sentence with the missing word
    /// 3. Displaying The Question/Missing word sentence and to check if you were right open the right markdown file.
    /// 
    /// Or we dont implement any of it and the user must find a work around himself. 
    /// 
    /// (We need a plugin system (Implement that one first))
    /// </summary>
    public interface MarkdownToSRSItems
    {
    }
}
