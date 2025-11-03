import {start} from "node:repl";


export function getPositionsNonContiguous(
    matrix :string[],
    sentence : string,
    caseInsensitive : boolean,
)
{
    const words = sentence.split(" ").filter(w => w.length > 0);
    const result = []
    let current = {row: 0, col: 0};
    for (let i = 0; i < words.length; i++) {
        const wordPositions = []
        const word = words[i];
        for (let j = 0; j < word.length; j++) {
            const char = word[j];
            const c = firstIndexOfMatrix(matrix, char, current, caseInsensitive);

            if (!c) {
                return result;
            }

            wordPositions.push(c);
            current = {row: c.row, col: c.col+1};
        }

        result.push(wordPositions);
    }

    return result;
}

export function getPositions(
    matrix: string[],
    sentence: string,
    caseInsensitive: boolean
):  {row: number, col: number}[][] {
    const result: {row: number, col: number}[][] = [];
    const tokens = sentence.split(" ").filter(w => w.length > 0);

    let current= {row: 0, col: 0};
    for (let i = 0; i < tokens.length; i++) {
        const c = firstIndexOfMatrix(matrix, tokens[i], current, caseInsensitive);
        if (!c)
        {
            break;
        }
        const wordPositions = [];
        //Assumes a single row, which is true for now
        for (let x = 0; x < tokens[i].length; ++x) {
            //TODO: More TS way to do this?
            wordPositions.push({row: c.row, col: c.col + x});
        }
        result.push(wordPositions)
        current = {row: c.row, col: c.col+1};
    }

    return result;
}

export function firstIndexOfMatrix(
    input: string[],
    search: string,
    startAt: {row:number, col:number},
    caseInsensitive: boolean
): {row:number, col:number} | null {

    if (!input || !search) {
        return null;
    }

    if (startAt.row > input.length) {
        return null;
    }

    if (startAt.col > input[startAt.row].length) {
        return null;
    }

    if (caseInsensitive) {
        for (let i = 0; i < input.length; ++i) {
            input[i] = input[i].toLowerCase();
        }
        search = search.toLowerCase();
    }

    const row = startAt.row
    let col = startAt.col
    for (let y = row; y < input.length; ++y) {
        for (let x = col; x < input[y].length; ++x) {
            let matchFound = true;
            for (let i = 0; i < search.length; ++i) {
                if (x + i > input[y].length) {
                    //Passed line
                    //for now, we're not crossing lines
                    matchFound = false;
                    break;
                }

                if (search[i] !== input[y][x+i]) {
                  //No match
                  matchFound = false;
                  break;
                }
            }
            //Match found
            if (matchFound) {
                return {row: y, col: x};
            }
        }

        //reset
        col = 0;
    }

    return null;
}
