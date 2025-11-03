'use client';

import React, { useState, useEffect } from 'react';
import { Lightbulb, Settings, Wifi, WifiOff, ChevronLeft, ChevronRight } from 'lucide-react';
import {getPositions, getPositionsNonContiguous} from "@/app/busi/busi";

export default function WordWall() {
    const [gridContent, setGridContent] = useState('HAPPY\nBIRTHDAY\nTO YOU');
    const [sentence, setSentence] = useState('');
    const [scale, setScale] = useState(0);
    const [wledIP, setWledIP] = useState('');
    const [wledConnected, setWledConnected] = useState(false);
    const [showSidePanel, setShowSidePanel] = useState(true);
    const [brightness, setBrightness] = useState(128);
    const [contiguousOnly, setContiguousOnly] = useState(false);

    const colors = [
        { bg: 'bg-red-400', text: 'text-gray-900', border: 'border-red-300', shadow: 'shadow-red-400/50', rgb: [255, 100, 100] },
        { bg: 'bg-blue-400', text: 'text-gray-900', border: 'border-blue-300', shadow: 'shadow-blue-400/50', rgb: [100, 150, 255] },
        { bg: 'bg-green-400', text: 'text-gray-900', border: 'border-green-300', shadow: 'shadow-green-400/50', rgb: [100, 255, 150] },
        { bg: 'bg-yellow-400', text: 'text-gray-900', border: 'border-yellow-300', shadow: 'shadow-yellow-400/50', rgb: [255, 255, 100] },
        { bg: 'bg-purple-400', text: 'text-gray-900', border: 'border-purple-300', shadow: 'shadow-purple-400/50', rgb: [200, 100, 255] },
        { bg: 'bg-pink-400', text: 'text-gray-900', border: 'border-pink-300', shadow: 'shadow-pink-400/50', rgb: [255, 150, 200] },
        { bg: 'bg-orange-400', text: 'text-gray-900', border: 'border-orange-300', shadow: 'shadow-orange-400/50', rgb: [255, 180, 100] },
        { bg: 'bg-teal-400', text: 'text-gray-900', border: 'border-teal-300', shadow: 'shadow-teal-400/50', rgb: [100, 220, 200] },
    ];

    const illuminateSentence = (sentence : string)=>
    {
        //Clear
        for (let y = 0; y < matrix.length; y++) {
            for (let x = 0; x < matrix[y].length; x++) {
                matrix[y][x].illuminated = false;
            }
        }


    }

    const sendToWLED = async () => {
        try {
            const ledData : number[] = [];

            matrix.forEach(row => {
                row.forEach(char => {
                    ledData.push(...colors[char.colorIndex].rgb);
                });
            });

            const response = await fetch(`http://${wledIP}/json/state`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    on: true,
                    bri: brightness,
                    seg: [{
                        i: ledData
                    }]
                })
            });

            if (response.ok) {
                console.log('Sent to WLED successfully');
            }
        } catch (error) {
            console.error('Failed to send to WLED:', error);
            setWledConnected(false);
        }
    };

    const testWLEDConnection = async () => {
        try {
            const response = await fetch(`http://${wledIP}/json/info`);
            if (response.ok) {
                setWledConnected(true);
                alert('Connected to WLED successfully!');
            } else {
                setWledConnected(false);
                alert('Failed to connect to WLED');
            }
        } catch (error) {
            setWledConnected(false);
            alert('Failed to connect to WLED. Make sure the IP is correct and WLED is accessible.');
        }
    };

    const matrix : {serial: number, illuminated: boolean, colorIndex : number, char : string}[][] = gridContent
        .split('\n')
        .filter(line => line.length > 0)
        .map((line) => line
            .trim()
            .split('')
            .map(char => {return {serial: -1, illuminated: false, colorIndex : -1, char : char}})
        )

    let serialIndex = 0;
    for (let y = 0; y < matrix.length; y++ ) {
        for (let x = 0; x < matrix[y].length; x++ ) {
            matrix[y][x].serial = serialIndex;
            serialIndex++;
        }
    }

    const lines = gridContent.split('\n');

    //word/letter matrix
    let positions : {row: number, col: number}[][]= []
    if (contiguousOnly) {
        positions = getPositions(lines, sentence, true)
    }
    else {
        positions = getPositionsNonContiguous(lines, sentence, true)
    }

    for (let wordIndex = 0; wordIndex < positions.length; wordIndex++) {
        for (let letterIndex = 0; letterIndex < positions[wordIndex].length; letterIndex++) {
            const letter = positions[wordIndex][letterIndex];
            matrix[letter.row][letter.col].illuminated = true;
            matrix[letter.row][letter.col].colorIndex = wordIndex % colors.length
        }
    }

    useEffect(() => {
        const handleResize = () => {

            const maxCols = Math.max(...matrix.map(r => r.length));
            const numRows = matrix.length;

            const availableWidth = window.innerWidth - (showSidePanel ? 384 : 0) - 100;
            const availableHeight = window.innerHeight - 100;

            const cellSize = 76;
            const requiredWidth = maxCols * cellSize;
            const requiredHeight = numRows * cellSize;

            const scaleX = availableWidth / requiredWidth;
            const scaleY = availableHeight / requiredHeight;

            setScale(Math.min(1, scaleX, scaleY));
        };
        handleResize()
        window.addEventListener('resize', handleResize);
        return () => window.removeEventListener('resize', handleResize);
    }, [gridContent, showSidePanel]);

    return (
        <div className="flex h-screen overflow-hidden bg-gradient-to-br from-gray-900 via-purple-900 to-gray-900">
            {/* Main Word Wall Area */}
            <div className="flex-1 flex items-center justify-center p-8 overflow-hidden">
                <div className="flex items-center justify-center">
                    <div
                        className="flex flex-col items-start gap-3"
                        style={{
                            transform: `scale(${scale})`,
                            transformOrigin: 'center center'
                        }}
                    >
                        {matrix.map((row, rowIndex) => (
                            <div key={rowIndex} className="flex gap-3">
                                {row.map((letter, colIndex) => {
                                    if (letter.char === ' ') {
                                        return <div key={colIndex} className="w-16 h-16" />;
                                    }

                                    const color = letter.illuminated ? colors[letter.colorIndex] : null;

                                    return (
                                        <div
                                            key={colIndex}
                                            className={`w-16 h-16 flex items-center justify-center font-bold text-5xl transition-all duration-300 ${
                                               color !== null 
                                                    ? `${color.text.replace('text-gray-900', `text-${color.bg.split('-')[1]}-400`)} drop-shadow-[0_0_12px_rgba(${color.rgb[0]},${color.rgb[1]},${color.rgb[2]},0.8)] scale-110`
                                                    : 'text-gray-700'
                                            }`}
                                            style={color !== null ? {
                                                textShadow: `0 0 20px rgb(${color.rgb[0]}, ${color.rgb[1]}, ${color.rgb[2]}), 0 0 40px rgb(${color.rgb[0]}, ${color.rgb[1]}, ${color.rgb[2]})`
                                            } : {}}
                                        >
                                            {letter.char}
                                        </div>
                                    );
                                })}
                            </div>
                        ))}
                    </div>
                </div>
            </div>

            {/* Side Panel */}
            <div className={`relative transition-all duration-300 ${showSidePanel ? 'w-96' : 'w-0'}`}>
                <button
                    onClick={() => setShowSidePanel(!showSidePanel)}
                    className="absolute left-0 top-1/2 -translate-y-1/2 -translate-x-full bg-gray-800 text-white p-2 rounded-l-lg hover:bg-gray-700 transition z-10"
                >
                    {showSidePanel ? <ChevronRight size={24} /> : <ChevronLeft size={24} />}
                </button>

                <div className={`h-full bg-gray-800 shadow-2xl overflow-y-auto transition-opacity duration-300 ${showSidePanel ? 'opacity-100' : 'opacity-0'}`}>
                    <div className="p-6 space-y-6">
                        <div className="text-center pb-4 border-b border-gray-700">
                            <h1 className="text-2xl font-bold text-white flex items-center justify-center gap-2">
                                <Lightbulb className="text-yellow-400" size={28} />
                                Word Wall
                            </h1>
                        </div>

                        <div>
                            <h2 className="text-lg font-semibold text-white mb-3">Message</h2>
                            <label className="block text-sm font-medium text-gray-300 mb-2">
                                Enter a sentence to illuminate
                            </label>
                            <input
                                type="text"
                                value={sentence}
                                onChange={(e) => setSentence(e.target.value)}
                                onKeyPress={(e) => e.key === 'Enter' && setSentence(sentence)}
                                placeholder="Type a message..."
                                className="w-full p-3 bg-gray-700 text-white rounded-lg border border-gray-600 focus:border-purple-500 focus:outline-none mb-3"
                            />
                            <div className="flex gap-2">
                                <button
                                    onClick={e => setSentence(sentence)}
                                    className="flex-1 px-4 py-3 bg-purple-600 text-white rounded-lg hover:bg-purple-700 transition font-semibold"
                                >
                                    Illuminate
                                </button>
                                <button
                                    onClick={() => {
                                        setSentence("")
                                        if (wledConnected && wledIP) {
                                            sendToWLED();
                                        }
                                    }}
                                    className="flex-1 px-4 py-3 bg-gray-600 text-white rounded-lg hover:bg-gray-700 transition font-semibold"
                                >
                                    Clear
                                </button>
                            </div>
                            <p className="text-xs text-gray-400 mt-2">
                                Each word will be displayed in a different color. {contiguousOnly ? 'Words must be formed from adjacent letters.' : 'Letters will be found sequentially in the grid.'}
                            </p>
                        </div>

                        <div className="border-t border-gray-700 pt-6">
                            <h2 className="text-lg font-semibold text-white mb-4 flex items-center gap-2">
                                <Settings size={20} />
                                Settings
                            </h2>

                            <div className="space-y-4">
                                <div>
                                    <label className="block text-sm font-medium text-gray-300 mb-2">
                                        Grid Content (use Enter for new lines, spaces for gaps)
                                    </label>
                                    <textarea
                                        value={gridContent}
                                        onChange={(e) => setGridContent(e.target.value)}
                                        className="w-full p-3 bg-gray-700 text-white rounded-lg border border-gray-600 focus:border-purple-500 focus:outline-none font-mono"
                                        rows={6}
                                        placeholder="Enter your word wall...&#10;One line per row"
                                    />
                                </div>

                                <div className="flex items-center gap-3">
                                    <input
                                        type="checkbox"
                                        id="contiguous"
                                        checked={contiguousOnly}
                                        onChange={(e) => setContiguousOnly(e.target.checked)}
                                        className="w-4 h-4 rounded"
                                    />
                                    <label htmlFor="contiguous" className="text-sm text-gray-300">
                                        Require contiguous letters
                                    </label>
                                </div>
                            </div>
                        </div>

                        <div className="border-t border-gray-700 pt-6">
                            <h2 className="text-lg font-semibold text-white mb-4 flex items-center gap-2">
                                {wledConnected ? <Wifi className="text-green-400" size={20} /> : <WifiOff className="text-gray-400" size={20} />}
                                WLED Integration
                            </h2>

                            <div className="space-y-4">
                                <div>
                                    <label className="block text-sm font-medium text-gray-300 mb-2">
                                        WLED IP Address
                                    </label>
                                    <input
                                        type="text"
                                        value={wledIP}
                                        onChange={(e) => setWledIP(e.target.value)}
                                        placeholder="192.168.1.100"
                                        className="w-full p-3 bg-gray-700 text-white rounded-lg border border-gray-600 focus:border-purple-500 focus:outline-none"
                                    />
                                </div>

                                <div>
                                    <label className="block text-sm font-medium text-gray-300 mb-2">
                                        Brightness: {brightness}
                                    </label>
                                    <input
                                        type="range"
                                        min="0"
                                        max="255"
                                        value={brightness}
                                        onChange={(e) => setBrightness(parseInt(e.target.value))}
                                        className="w-full"
                                    />
                                </div>

                                <button
                                    onClick={testWLEDConnection}
                                    className="w-full px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 transition"
                                >
                                    Test Connection
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}