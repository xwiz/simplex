After studying the data for some time, the following observations were made:
 - Reserve Listings consistently start with the reserve date details
 - ShelfLocation and Barcode information are always present
 - Certain information such as Author information may be missing
 - The different data stored have specific formats

The algorithm has been thus developed to extract information from input stream in the following manner:
 - Keep track of currently extracted item info
 - Loop through characters in input stream
 - Aggregate non whitespace characters until large space encountered
 - If date encountered, begin new reserve detail capture
 - Else
 	- Attempt to extract item based on item extract index 
	- If format check fails and item must not be present, move to next item
	
 - Loop until all reserves and items have been extracted and stored