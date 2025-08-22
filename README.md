# PolyKiteFun

This is a project which explores all possible [polykites](https://mathworld.wolfram.com/Polykite.html) and their corresponding [Heesch numbers](https://mathworld.wolfram.com/HeeschNumber.html).


Polykites are usually defined by taking one sixth of a hexagon from a grid of hexagons, and if you increase the number of kites which are next to each other, you get a polykite.


By creating polykites in this way you are constrained to that hexagon grid, and thus you get for n kites m ways of arranging them in that grid.


The numbers of polykites with n=1, 2, ... components are 1, 2, 4, 10, 27, 85, 262, ... [OEIS A057786](http://oeis.org/A057786)


But, this project explores polykites which are not constrained by the hexagon grid, but instead they are bound by them selves, i.e. their edges of the same length has to match.


When doing things this way the numbers of polykites with n=1, 2, ... components are 1, 4, 14, 86, 524, 3661, 25637, 185374, ...


Examples where n is 2:


<img width="328" height="97" alt="Polykites-2" src="https://github.com/user-attachments/assets/84b93582-06bf-4702-8f2f-dba24f3890c0" />


n is 3:


<img width="574" height="182" alt="Polykites-3" src="https://github.com/user-attachments/assets/fb914fcc-14e0-480e-a5fa-5525b23ae368" />


n is 4:


<img width="881" height="725" alt="Polykites-4" src="https://github.com/user-attachments/assets/32b5e488-607e-4542-91a1-f249b5cd845a" />


## Examples of found combinations and their Heesch numbers

You can find examples of up to Heesch number 3 for n 2,3 and 4 in `combinations` folder of this repository.


Because the number of combinations and their Heesch number solutions grow rapidly, the solutions for n 5,6,7, and 8 are located [here](https://mega.nz/folder/fbQ2xRwR#N3abQX7fteMSIuXKpjWWxQ).

One interesting thing you can find there for n = 8, is that the solution number 181053 is the [Einstein tile](https://www.scientificamerican.com/article/newfound-mathematical-einstein-shape-creates-a-never-repeating-pattern/)

## Usage

This .NET 9.0 project is tested only on Windows but it should be cross platfom. You can find the binary for windows in the releases page here in github.


When you start the program, it will ask you how many kites are you willing to combine in a single tile, this represents the polykite of number n.


Then it will ask you up to which Heesch number are you willing to explore those tiles/polykites.


Once all of the combinations for n kites are found you can pick one solution to explore or you can process all of those polykites.


Found solutions are saved in the json file so you do not need to find those polykites again, for example combinations_2.json is a json file which saves all of the information for polykites where n = 2.


The folder structure of found polykites and their corresponding Heesch numbers is this: combinations/{images of polykites}/{HeeschNumber 1,2,3...}


