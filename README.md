# Overdrive Audiobook Splitter

Overdrive audiobooks are delivered by mp3 files with multiple parts. Each file contains multiple chapters, usually over an hour in length.

This CLI application splits the files into their chapters using the embedded metadata.

`Book-Part1.mp3` `=>` `Book-000-Chapter1.mp3, Book-001-Chapter2.mp3`, etc.
