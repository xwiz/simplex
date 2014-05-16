A simple documented OOP code demonstration of file/text processing using C#
Project for Shoppi.ng

The aim of the exercise is to extract and organized input data into plain CSV format for Excel

The code consists of two main parts The input reader/extractor and the output writer.
Information is read using an in-memory FileStream for speed, processed and kept in memory until final output.

If the input is a file, the data should be in UTF8 Encoding.

Usage sample:

Enter file path to process: data.txt
 - Result will be dumped in output.txt and opened for viewing