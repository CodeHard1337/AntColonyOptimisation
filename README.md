# Ant Colony Optimisation

Ant Colony Optimisation is a solution to the traveling salesman problem (TSP). TSP is a problem where a path needs to be found between different waypoints. This may sound easy to do, but without any optimisations, finding the shortest path possible has a complexity of O(n!). This means that for just 20 waypoints, it may already take a few hundred years to find the optimal solution.

Ant Colony Optimisation shortens the calculation time by a lot (but does not garantee the shortest possible path will be returned). It does this in iterations. Each iteration, a few virtual ants get set loose on the graph. They try to find a semi-random path, by always choosing the next node based on distance and pheremones left by previous ants. After the ants have found their path, they leave an amount of pheremones inversely proportional to the length of the path. After a predefined number of iterations, the path calculated will be very short for the amount of time it took to calculate.


### Step 1: Calculating the path of one ant

The ant will need to find a semi-random path on the graph, so it will be dropped on a random waypoint.

ant_start.png

It will then need to choose what waypoint it will visit next. It does this by looking at two things: the distance to the waypoint, and the amount of pheremones left by other ants.

ant_distance.png ant_pheremones.png

The ant adds up those two measurements (scaled with the settings, so it may take the pheremones more into account than the distance).

ant_distance_pheremones.png

It then uses this result as a probability for choosing the next waypoint.

ant_next_waypoint.png

This process gets repeated until the ant has visited every waypoint. After that, the ant goes back to its starting position.

ant_path.png

It looks at the path it took, and calculates the distance. It then leaves pheremones along the path inversely proportional to that distance. This will encourage other ants to follow this path if it is short, or discourage them if it is long.

ant_path_pheremones.png

### Step 2: Calculating iterations

One iteration consists of multiple ants looking for a path independently of each other. The pheremones that were already there first evaporate a bit. Then all the ants from this iteration calculate their path, and add their pheremones. After that if we found a shorter path than the one already saved, it gets saved instead. This gets done a set amount of times, after which we will hopefully have a good path.

### Step 3: 2-OPT optimisation (optional)

When the path is fully calculated by the ants there may still be some parts that cross over each other, which is obviously not the shortest possible route. 2-OPT tries to eliminate those cross-overs. For each possible combination of two nodes it switches their possition in the path.

2_opt_before.png 2_opt_after.png

If the resulting path is shorter, it gets saved. This gets repeated a set amount of times, at the end of which we will have a path that is shorter than the ants alone could have calculated.
