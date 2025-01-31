using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Words;

public class qwertyCoordinates : MonoBehaviour {

    public KMBombModule module;
    public KMBombInfo bomb;
    public KMAudio bombAudio;
    public KMSelectable moduleSelectable;
    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool focused;
    private bool solved;
    private bool reset;

    //visuals
    public Text puzzleText;
    public Text[] puzzleOutlineText;
    //public Text answerText;
    public GameObject answerTextParent;
    public GameObject answerTextPrefab;
    public GameObject answerIconPrefab;
    List<SpriteRenderer> answerTextRenderers = new List<SpriteRenderer>();
    List<SpriteRenderer> answerIconRenderers = new List<SpriteRenderer>();
    
    //sprites
    public Sprite answerDashUppercase;
    public Sprite answerDashLowercase;
    public Sprite[] answerUppercaseLetters;
    public Sprite[] answerLowercaseLetters;
    public Sprite answerCapsLock;
    public Sprite answerShift;
    float spriteWidth = .25f;

    //background elements
    public MeshRenderer backgroundStarRenderer;
    Material backgroundStarMaterial;

    //puzzle generation
    string puzzle = "";
    List<string> answer = new List<string>();
    string formattedAnswer = "";
    int totalAnswerLength = 0;
    int desiredLength = 10;
    char[,] keyboardLayout = new char[3, 11]
    {
        { 'T','q','w','e','r','t','y','u','i','o','p' },
        { 'C','a','s','d','f','g','h','j','k','l',';' },
        { 'S','z','x','c','v','b','n','m',',','.','/' }
    };

    //submission
    private KeyCode[] TypableKeys =
    {
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T, KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P,
        KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L,
        KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.M,
    };
    private string keyNames = "qwertyuiopasdfghjklzxcvbnm";
    private KeyCode[] ShiftKeys =
    {
        KeyCode.LeftShift, KeyCode.RightShift
    };

    //input
    string inputtedText = "";
    int inputLength = 0;
    int inputIndex = 0;
    bool capsLock = false;
    bool switchCapsLock = false;
    bool shift = false;
    bool tabOn = false;
    private string alphabet = "abcdefghijklmnopqrstuvwxyz";

    void Awake()
    {
        ModuleId = ModuleIdCounter++;

        moduleSelectable.OnFocus += delegate { focused = true; };
        moduleSelectable.OnDefocus += delegate { focused = false; };
    }

    void Start () {
        generatePuzzle();

        puzzleText.text = puzzle;
        for(int i = 0; i < 8; i++)
        {
            puzzleOutlineText[i].text = puzzle;
        }

        /*answerText.text = "";
        for(int i = 0; i < answer.Count; i++)
        {
            for(int j = 0; j < answer[i].Length; j++)
            {
                if(char.IsUpper(answer[i][j]))
                {
                    answerText.text += "<color=magenta>_</color>";
                }
                else
                {
                    answerText.text += "_";
                }
            }
            answerText.text += " ";
        }*/

        instantiateDashes();

        backgroundStarMaterial = new Material(backgroundStarRenderer.material);
        backgroundStarRenderer.material = backgroundStarMaterial;
        StartCoroutine(WaveStar());
    }

    void Update()
    {
        if (focused && !solved)
        {
            if(Input.anyKeyDown && reset)
            {
                resetDashes();
            }

            for(int i = 0; i < TypableKeys.Length; i++)
            {
                if(Input.GetKeyDown(TypableKeys[i]))
                {
                    inputCharacter(keyNames[i]);
                }
            }
            for (int i = 0; i < ShiftKeys.Length; i++)
            {
                if (Input.GetKeyUp(ShiftKeys[i]))
                {
                    //print("Shift off");
                    shift = false;

                    if(!switchCapsLock && inputLength < answerIconRenderers.Count) //if switchCapsLock is true, we know capsLock was pressed so we shouldn't blank the sprite
                    {
                        answerIconRenderers[inputLength].sprite = null;
                    }
                }
                if (Input.GetKeyDown(ShiftKeys[i]))
                {
                    //print("Shift on");
                    if (!tabOn)
                    {
                        switchCapsLock = false;
                        shift = true;

                        answerIconRenderers[inputLength].sprite = answerShift;
                    }
                    else
                    {
                        bombAudio.PlaySoundAtTransform("buzzer", transform);
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.CapsLock))
            {
                //print("Caps " + (switchCapsLock ? "off" : "on"));
                if (!tabOn)
                {
                    switchCapsLock = !switchCapsLock;
                    shift = false;

                    if (inputLength < answerIconRenderers.Count)
                    {
                        if (switchCapsLock)
                        {
                            answerIconRenderers[inputLength].sprite = answerCapsLock;
                        }
                        else
                        {
                            answerIconRenderers[inputLength].sprite = null;
                        }
                    }
                }
                else
                {
                    bombAudio.PlaySoundAtTransform("buzzer", transform);
                }
            }
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                deleteCharacter();
            }
        }
    }

    void inputCharacter(char input)
    {
        string addedText = "";

        //note: only one of these can happen at a time
        //if we need to switch capslock, do so
        if (switchCapsLock)
        {
            addedText += "C";
            capsLock = !capsLock;
        }
        //if we need to activate shift, mark that
        if (shift)
        {
            addedText += "S";
        }
        if(tabOn)
        {
            addedText += "T";
        }
        bool capitalized = capsLock ^ shift;
        addedText += input;

        //print("Expected letter: " + formattedAnswer[inputLength]);
        //print("Inputted letter: " + input);

        //capitalization needs to match to actually input the letter
        if (capitalized != char.IsUpper(formattedAnswer[inputIndex]))
        {
            //print("Capitalizaion doesn't match");

            if(switchCapsLock)
            {
                capsLock = !capsLock;
            }

            bombAudio.PlaySoundAtTransform("buzzer", transform);
            return;
        }

        //set sprites, they are different for uppercase and lowercase
        if(capitalized)
        {
            answerTextRenderers[inputLength].sprite = answerUppercaseLetters[alphabet.IndexOf(input)];
        }
        else
        {
            answerTextRenderers[inputLength].sprite = answerLowercaseLetters[alphabet.IndexOf(input)];
        }

        //reset variables
        switchCapsLock = false;
        tabOn = false;

        //add on what was just inputted
        inputLength++;
        inputIndex++;
        inputtedText += addedText;

        //if input is complete, check the answer
        if (inputIndex == formattedAnswer.Length)
        {
            checkAnswer();
            return;
        }

        //if the character is now a tab character, advance the index by one and turn on tabOn
        if (formattedAnswer[inputIndex] == ' ')
        {
            //print("let's go tabbing!!");
            tabOn = true;
            shift = false;
            inputIndex++;
        }

        if(shift)
        {
            answerIconRenderers[inputLength].sprite = answerShift;
        }
    }

    void deleteCharacter()
    {
        if (inputtedText.Length == 0)
        {
            return;
        }

        answerIconRenderers[inputLength].sprite = null;

        //tab shenanigans need to be dealt with first otherwise inputIndex will be too far forward
        //if the last character was a tab, tabOn needs to be reenabled
        if (inputtedText.Length >= 2) //if the inputted text has too low a length there isn't even gonna be a tab character anyway
        {
            if (inputtedText[inputtedText.Length - 2] == 'T')
            {
                shift = false;
                switchCapsLock = false;
                //print("Tab is now on");
                tabOn = true;
                shift = false;
            } //if tabOn is on, we need to deincrement inputIndex by one more and turn it off
            else if (tabOn)
            {
                //print("Tab is now off");
                tabOn = false;
                inputIndex--;
            }
        }

        //print((inputIndex - 1) + " :) " + formattedAnswer[(inputIndex - 1)]);
        if (char.IsUpper(formattedAnswer[inputIndex - 1]))
        {
            answerTextRenderers[inputLength - 1].sprite = answerDashUppercase;
        }
        else
        {
            answerTextRenderers[inputLength - 1].sprite = answerDashLowercase;
        }

        inputLength--;
        inputIndex--;
        inputtedText = inputtedText.Remove(inputtedText.Length - 1);

        //if shift is being held, signify that
        if (shift)
        {
            answerIconRenderers[inputLength].sprite = answerShift;
        }

        if (inputtedText.Length <= 0)
        {
            return;
        }

        //if the last character was also a caps lock change, undo that
        if (inputtedText[inputtedText.Length - 1] == 'C')
        {
            capsLock = !capsLock;
            shift = false;
            switchCapsLock = true;
        }
        else
        {
            //this is probably super necessary i thjink
            switchCapsLock = false;
        }

        //if the last character was a shift, make its icon blank
        if (inputtedText[inputtedText.Length - 1] == 'S' && !shift)
        {
            answerIconRenderers[inputLength].sprite = null;
        }

        //remove special characters
        if ("TCS".Contains(inputtedText[inputtedText.Length - 1].ToString()))
        {
            inputtedText = inputtedText.Remove(inputtedText.Length - 1);
        }
    }

    void checkAnswer()
    {
        LogMsg("Submitted " + inputtedText);

        bool incorrectLetter = false;
        List<int> correctLetterIndices = new List<int>();
        for(int i = 0; i < inputtedText.Length; i += 2)
        {
            if(i + 1 >= inputtedText.Length)
            {
                LogMsg("Ending character " + inputtedText[i] + " doesn't have another character to make a coordinate.");
                incorrectLetter = true;
                break;
            }
            char letterFromInput = keyboardLayout[RowFromCharacter(inputtedText[i]), IndexFromCharacter(inputtedText[i + 1])];
            if (inputtedText[i] == inputtedText[i + 1]) //incorrect letter
            {
                LogMsg("Coordinate " + inputtedText[i] + inputtedText[i + 1] + " both are the same letter.");
                incorrectLetter = true;
            }
            else if (RowFromCharacter(inputtedText[i]) == RowFromCharacter(inputtedText[i + 1]) || IndexFromCharacter(inputtedText[i]) == IndexFromCharacter(inputtedText[i + 1])) //incorrect letter
            {
                LogMsg("Coordinate " + inputtedText[i] + inputtedText[i + 1] + "'s characters are the same row/column.");
                incorrectLetter = true;
            }
            else if (letterFromInput != char.ToLower(puzzle[i / 2])) //incorrect letter
            {
                LogMsg("Coordinate " + inputtedText[i] + inputtedText[i + 1] + " results in the character " + letterFromInput + ", which is incorrect. (Expected character: " + char.ToLower(puzzle[i / 2]) + ")");
                incorrectLetter = true;
            }
            else
            {
                correctLetterIndices.Add(i);
                correctLetterIndices.Add(i+1);
            }
        }

        if(incorrectLetter)
        {
            reset = true;
            module.HandleStrike();

            //visuals
            int answerLetterIndex = 0;
            for(int i = 0; i < inputtedText.Length; i++)
            {
                if(inputtedText[i].EqualsAny('T','C','S'))
                {
                    continue;
                }

                //set incorrect coordinate pairs to red and correct ones to green
                if (correctLetterIndices.Contains(i))
                {
                    answerTextRenderers[answerLetterIndex].color = Color.green;
                    answerIconRenderers[answerLetterIndex].color = Color.green;
                }
                else
                {
                    answerTextRenderers[answerLetterIndex].color = Color.red;
                    answerIconRenderers[answerLetterIndex].color = Color.red;
                }

                answerLetterIndex++;
            }

            return;
        }

        bool invalidWord = false;
        string[] inputtedAnswer = inputtedText.Replace("C","").Replace("S", "").Split('T'); //splits the inputted text at the tabs so we can check if each word is in our wordlist
        int otherAnswerLetterIndex = 0; //this is only named differently because wawawa variables have to be named differently wawawa
        for (int i = 0; i < inputtedAnswer.Length; i++)
        {
            if (new Data().ContainsWord(inputtedAnswer[i]))
            {
                for (int j = 0; j < inputtedAnswer[i].Length; j++, otherAnswerLetterIndex++)
                {
                    answerTextRenderers[otherAnswerLetterIndex].color = Color.green;
                    answerIconRenderers[otherAnswerLetterIndex].color = Color.green;
                }
            }
            else
            {
                invalidWord = true;
                for (int j = 0; j < inputtedAnswer[i].Length; j++, otherAnswerLetterIndex++)
                {
                    answerTextRenderers[otherAnswerLetterIndex].color = Color.yellow;
                    answerIconRenderers[otherAnswerLetterIndex].color = Color.yellow;
                }
            }
        }

        if(invalidWord)
        {
            reset = true;
            return;
        }

        module.HandlePass();
        solved = true;
        puzzleText.color = Color.green;
        backgroundStarMaterial.color = Color.green;
    }

    void resetDashes()
    {
        reset = false;
        inputIndex = 0;
        inputLength = 0;
        inputtedText = "";
        capsLock = false;

        int index = 0;
        for (int i = 0; i < answer.Count; i++)
        {
            //print("Word " + i + " (" + answer[i] + ")");

            for (int j = 0; j < answer[i].Length; j++, index++)
            {
                //print("j=" + j + " : index=" + index);
                if (char.IsUpper(answer[i][j]))
                {
                    answerIconRenderers[index].color = Color.cyan;
                    answerTextRenderers[index].sprite = answerDashUppercase;
                    answerTextRenderers[index].color = Color.cyan;
                }
                else
                {
                    answerIconRenderers[index].color = Color.white;
                    answerTextRenderers[index].sprite = answerDashLowercase;
                    answerTextRenderers[index].color = Color.white;
                }
                answerIconRenderers[index].sprite = null;
            }
        }
    }

    void generatePuzzle()
    {
        //generate the answer
        while(Mathf.Abs(desiredLength - totalAnswerLength) >= 3 && totalAnswerLength < desiredLength) //puzzle is about the desired length
        {
            int length = Random.Range(3, Mathf.Min(9, desiredLength - totalAnswerLength));
            string word = new Data().PickWord(length).ToLower();
            if((answer.Count == 0 || RowFromCharacter(word[0]) != 0) && !word.ContainsIgnoreCase("p")) //after the first word, a tab character will be added, so the next word cannot start with a letter on the top row. p is avoided because its impossible to generate a puzzle with exclusively letters when using p
            {
                answer.Add(word);
                totalAnswerLength += word.Length;
            }
        }
        //answer.Shuffle();
        //this commented out piece of code is used for debugging
        //answer = new List<string> { "sleek", "aye" };

        //create the log for the answer
        LogMsgSilent("Trying answer: " + FormatStringList(answer));

        bool initialCapsLockState = false;
        //generate the puzzle based on the answer
        for(int i = 0; i < answer.Count; i++) //for every word in the answer
        {
            string currentWord = answer[i];
            List<int> availablePairs = new List<int>(); //every adjacent pair is listed by its first index
            for(int j = 0; j < currentWord.Length - 1; j++) //setup list
            {
                if(j == 0 && i > 0)  //every word but the first has a preceding tab character, which means its not a part of a letter pair
                {
                    continue;
                }
                availablePairs.Add(j);
            }

            //use multiple techniques to prune pairs that will cause problems/bad puzzles
            for(int j = 0; j < availablePairs.Count; j++)
            {
                int currentIndex = availablePairs[j];
                //I/K/O/L cannot use the bottom row in the puzzle
                if ("iokl".Contains(currentWord[currentIndex].ToString()) && RowFromCharacter(currentWord[currentIndex + 1]) == 2)
                {
                    availablePairs.Remove(currentIndex);
                    j--;
                    continue;
                }
                //there cannot be two consecutive characters on the same row or column, or else you'll pretty much always get bs puzzles
                if (IndexFromCharacter(currentWord[currentIndex]) == IndexFromCharacter(currentWord[currentIndex + 1]) ||
                    RowFromCharacter(currentWord[currentIndex]) == RowFromCharacter(currentWord[currentIndex + 1]))
                {
                    availablePairs.Remove(currentIndex);
                    j--;
                    continue;
                }
                //don't want punctuation in puzzle
                if(RowFromCharacter(currentWord[currentIndex]) == 2 && IndexFromCharacter(currentWord[currentIndex + 1]) >= 8)
                {
                    availablePairs.Remove(currentIndex);
                    j--;
                    continue;
                }
            }

            //L and K are especially constrained because they cannot use capslock or shift
            //we need to check that they have pairs and if not regenerate the entire puzzle
            //this does not apply if L or K is the first letter of the non-first word
            if(currentWord.Contains("k") || currentWord.Contains("l"))
            {
                for (int j = 0; j < currentWord.Length; j++)
                {
                    if(j == 0 && i > 0)
                    {
                        continue;
                    }

                    if ("kl".Contains(currentWord[j].ToString()))
                    {
                        if (availablePairs.Contains(j) && !availablePairs.Contains(j - 1) && availablePairs.Contains(j + 1)) //there is exclusively one pair that contains this letter, and k/l is first, remove the pair that will use the second letter otherwise (if it exists)
                        {
                            availablePairs.Remove(j + 1);
                        }
                        else if(!availablePairs.Contains(j) && availablePairs.Contains(j - 1) && availablePairs.Contains(j - 2)) //there is exclusively one pair that contains this letter, and k/l is second, follow a similar protocol
                        {
                            availablePairs.Remove(j - 2);
                        }
                        else if(!availablePairs.Contains(j) && !availablePairs.Contains(j - 1)) //if there are no pairs that contain this letter the puzzle is gauranteed to be bad and we must start over
                        {
                            //clear all variables
                            puzzle = "";
                            answer.Clear();
                            totalAnswerLength = 0;
                            
                            generatePuzzle();
                            return;
                        }
                    }
                }
            }

            /*for (int j = 0; j < availablePairs.Count; j++)
            {
                print( "CONFIRMED PAIR " + availablePairs[j]);
            }*/

            ////begin adding in extra characters
            string editedWord = currentWord;
            int editedWordOffset = 0;

            //words w/ an odd number of characters need an extra shift/capslock character

            for (int j = 0; j < currentWord.Length; j++)
            {
                if (j == 0 && i > 0) //every word but the first has a preceding tab character
                {
                    editedWordOffset++;
                    editedWord = "T" + editedWord;
                    continue;
                }
                else if (!availablePairs.Contains(j) || //no pair has this letter, we must caps/shift
                (j == currentWord.Length - 1 && editedWord.Length % 2 == 1)) // or we are at the end of the word and it's currently odd
                {
                    if (RowFromCharacter(editedWord[j + editedWordOffset]) == 2 || IndexFromCharacter(editedWord[j + editedWordOffset]) >= 8) //if shift shares the row or if the letter causes punctation we can't use shift
                    {
                        editedWord = editedWord.Insert(j + editedWordOffset, "C");
                    }
                    else if (RowFromCharacter(editedWord[j + editedWordOffset]) == 1) //overlap, can't use caps lock
                    {
                        editedWord = editedWord.Insert(j + editedWordOffset, "S");
                    }
                    else
                    {
                        switch (Random.Range(0, 2))
                        {
                            case 0: //shift
                                editedWord = editedWord.Insert(j + editedWordOffset, "S");
                                break;
                            default: //caps
                                editedWord = editedWord.Insert(j + editedWordOffset, "C");
                                break;
                        }
                    }
                    editedWordOffset++;
                    continue;
                }
                else //we allow the pair
                {
                    j++; //advance j by one so we won't consider the next letter since we just confirmed it as the pair for the last one we checked
                }
            }
            print("Edited word: " + editedWord);

            ////create puzzle based off of modified answer
            bool capsOn = initialCapsLockState;
            for(int j = 0; j < editedWord.Length; j += 2) //because of added tabs/shift/capslock characters word will have an even length
            {
                LogMsgSilent("character pair: \'" + editedWord[j] + "\' \'" + editedWord[j + 1] + "\'");
                string addOn = "";
                switch(editedWord[j])
                {
                    case 'T':
                        addOn += keyboardLayout[0,IndexFromCharacter(editedWord[j + 1])];
                        break;
                    case 'C':
                        capsOn = !capsOn;
                        addOn += keyboardLayout[1, IndexFromCharacter(editedWord[j + 1])];
                        break;
                    case 'S':
                        addOn += keyboardLayout[2, IndexFromCharacter(editedWord[j + 1])];
                        break;
                    default: //presumably a letter
                        addOn += keyboardLayout[RowFromCharacter(editedWord[j]), IndexFromCharacter(editedWord[j + 1])];
                        break;
                }
                puzzle += addOn;
            }

            string formattedWord = FormatEditedWord(editedWord, initialCapsLockState);
            answer[i] = formattedWord;
            initialCapsLockState = capsOn;
        }

        puzzle = puzzle.ToUpper();
        LogMsg("The puzzle is " + puzzle);
        formattedAnswer = FormatStringList(answer);
        LogMsg("An acceptable answer for this puzzle would be " + formattedAnswer);
    }

    void instantiateDashes() //creates the visuals for the answer lengths centered around the answerTextParent object
    {
        int characterCount = 0;
        for (int i = 0; i < answer.Count; i++)
        {
            if (characterCount > 0) //not the start of a line
            {
                characterCount++;
            }
            characterCount += answer[i].Length;

            //print("Character count: " + characterCount);
        }
        float lineWidth = (characterCount - 1) * spriteWidth;

        int index = 0;
        for (int i = 0; i < answer.Count; i++)
        {
            //print("Word " + i + " (" + answer[i] + ")");

            for(int j = 0; j < answer[i].Length; j++,index++ )
            {
                if(i > 0 && j == 0)
                {
                    index++;
                }

                //print("j=" + j + " : index=" + index);

                GameObject characterObject = Instantiate(answerTextPrefab, answerTextParent.transform.TransformPoint(new Vector3(Mathf.Lerp(lineWidth / -2f, lineWidth / 2f, (float)index/(characterCount - 1)),0,0)), answerTextParent.transform.rotation, answerTextParent.transform );
                SpriteRenderer newRenderer = characterObject.GetComponent<SpriteRenderer>();
                answerTextRenderers.Add(newRenderer);
                SpriteRenderer newIconRenderer = characterObject.transform.GetChild(0).GetComponent<SpriteRenderer>();
                answerIconRenderers.Add(newIconRenderer);

                if (char.IsUpper(answer[i][j]))
                {
                    newIconRenderer.color = Color.cyan;
                    newRenderer.sprite = answerDashUppercase;
                    newRenderer.color = Color.cyan;
                }
            }
        }

        if(characterCount >= 10)
        {
            //print(characterCount);
            /*TODO: Implement*/
            answerTextParent.transform.localScale = new Vector3(.115f - (.005f * characterCount), 0.07f, 0.07f);
        }
    }

    int IndexFromCharacter(char letter)
    {
        switch(letter)
        {
            case 'q':
            case 'a':
            case 'z':
                return 1;
            case 'w':
            case 's':
            case 'x':
                return 2;
            case 'e':
            case 'd':
            case 'c':
                return 3;
            case 'r':
            case 'f':
            case 'v':
                return 4;
            case 't':
            case 'g':
            case 'b':
                return 5;
            case 'y':
            case 'h':
            case 'n':
                return 6;
            case 'u':
            case 'j':
            case 'm':
                return 7;
            case 'i':
            case 'k':
                return 8;
            case 'o':
            case 'l':
                return 9;
        }
        return 10;
    }

    int RowFromCharacter(char letter)
    {
        switch (letter)
        {
            case 'T':
            case 'q':
            case 'w':
            case 'e':
            case 'r':
            case 't':
            case 'y':
            case 'u':
            case 'i':
            case 'o':
            case 'p':
                return 0;
            case 'C':
            case 'a':
            case 's':
            case 'd':
            case 'f':
            case 'g':
            case 'h':
            case 'j':
            case 'k':
            case 'l':
                return 1;
            case 'S':
            case 'z':
            case 'x':
            case 'c':
            case 'v':
            case 'b':
            case 'n':
            case 'm':
                return 2;
        }
        return 3;
    }

    string FormatEditedWord(string text, bool initialCapsLock)
    {
        string editedText = "";

        bool capsLock = initialCapsLock;
        bool shift = false;
        for(int i = 0; i < text.Length; i++)
        {
            switch(text[i])
            {
                case 'C':
                    capsLock = !capsLock;
                    break;
                case 'S':
                    shift = true;
                    break;
                case 'T': //tab: don't add anything because this is already dealt with
                    break;
                default:
                    if(capsLock ^ shift)
                    {
                        editedText += text[i].ToString().ToUpper();
                    }
                    else //everything is formatted in lowercase so we don't need to convert
                    {
                        editedText += text[i];
                    }
                    shift = false;
                    break;
            }
        }
        return editedText;
    }

    IEnumerator WaveStar()
    {
        float tx = 0;
        float ty = 0;

        while(true)
        {
            backgroundStarMaterial.SetFloat("_XPhase", tx);
            backgroundStarMaterial.SetFloat("_YPhase", ty);

            tx += .01f;
            ty += .0105f;
            yield return new WaitForSeconds(.01f);
        }
    }
    
    void LogMsg(string msg)
    {
        Debug.LogFormat("[QWERTY Coordinates #{0}] {1}", ModuleId , msg);
    }

    void LogMsgSilent(string msg)
    {
        Debug.LogFormat("<QWERTY Coordinates #{0}> {1}", ModuleId, msg);
    }

    string FormatStringList(List<string> list)
    {
        string formattedList = "";
        for (int i = 0; i < answer.Count; i++)
        {
            if (i > 0)
            {
                formattedList += " ";
            }
            formattedList += answer[i];
        }
        return formattedList;
    }
}
