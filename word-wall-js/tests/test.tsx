import {describe, expect, test} from '@jest/globals';
import {firstIndexOfMatrix, getPositionsNonContiguous} from "../app/busi/busi";

describe('finding words', () => {
    test('zero index word', () => {
        const index = firstIndexOfMatrix(["abcdefghijklmnop"], "abc", {row: 0, col:0}, true)
        expect(index).toEqual({row: 0, col:0})
    });

    test('nonzero index word', () => {
        const index = firstIndexOfMatrix(["abcdefghijklmnop"], "def",  {row: 0, col:0}, true)
        expect(index).toEqual({row: 0, col:3})
    });

    test('nonzero index word with start at', () => {
        const index = firstIndexOfMatrix(["abcdefghijklmnop"], "fgh",  {row: 0, col:3}, true)
        expect(index).toEqual({row: 0, col:5})
    });

    test('nonzero index word with start at multi row', () => {
        const index = firstIndexOfMatrix(["abcdefghijklmnop", "test"], "test",  {row: 0, col:3}, true)
        expect(index).toEqual({row: 1, col:0})
    });

    test('not found after start index', () => {
        const index = firstIndexOfMatrix(["abcdefghijklmnop"], "abc",  {row: 0, col:3}, true)
        expect(index).toEqual(null)
    });

    test('not found ', () => {
        const index = firstIndexOfMatrix(["abcdefghijklmnop"], "notfound", {row: 0, col:3}, true)
        expect(index).toEqual(null)
    });

    test('non contiguous', () => {
        const index = getPositionsNonContiguous(["abcd","efgh","ijkl","mnop"], "ab cd e i", true)
        expect(index).toEqual([[{row: 0, col:0},{row: 0, col:1}],[{row: 0, col:2},{row: 0, col:3}],[{row: 1, col:0}],[{row: 2, col:0}]])
    });

    test('happy birthday', () => {
        const index = getPositionsNonContiguous(["HAPPY","BIRTHDAY","TO YOU"], "HAPPY", true)
        expect(index).toEqual([[{row: 0, col:0},{row: 0, col:1},{row: 0, col:2},{row: 0, col:3},{row: 0, col:4}]])
    });

    test('happy birthday multi line', () => {
        const index = getPositionsNonContiguous(["HAPPY","BIRTHDAY","TO YOU"], "HAPPY BIR", true)
        expect(index).toEqual([[{row: 0, col:0},{row: 0, col:1},{row: 0, col:2},{row: 0, col:3},{row: 0, col:4}],[{row: 1, col:0},{row: 1, col:1},{row: 1, col:2}]])
    });
});