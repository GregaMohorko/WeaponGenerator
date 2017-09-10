# WeaponGenerator

An implementation of [**Information Extraction Over the Internet for a Dynamic Game**](https://pdfs.semanticscholar.org/e8a2/1d38476f84e25fed7d040219916342413b6b.pdf) paper by Simon Cutajar, where information retrieval over the internet and information extraction is used to generate medieval weapons that could be used in a dynamic game.

Implementation is done in .NET C# and implements only the *information extraction* part of the paper, with a few minor modifications for improvement.

## Results

Results of this program can be seen in [weapons.txt](Results/weapons.txt). There you can find all the weapons generated from running the program one single time (900+ weapons).

Note that it generates only medieval weapons (no pistols or anything mechanical) and only land weapons (no ships or planes).

## Running and testing

Running the program can be done by executing the `WeaponGenerator.exe` file in **/Builds** folder.

Note that running the program from zero can be quite time consuming (~1h), depending mostly on the speed of the internet connection.

## Author and License

Grega Mohorko ([www.mohorko.info](https://www.mohorko.info))

Copyright (c) 2017 Grega Mohorko

[MIT License](./LICENSE)
