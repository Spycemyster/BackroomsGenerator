# Maze Generation Algorithm
## Abstract
Our goal is to procedurally generate an **infinite** maze for the player to roam around in. We can define our maze as a graph where nodes represent chunks and edges represent an opening between the two nodes. We should also define properties such as depth (how far from the origin the node is).

## Pseudocode
* Define $G(E,V)$ as a grid of nodes with edges that represents an opening between the two nodes.
* Define $P=(P_x,P_y)$ as the player position.
* Define $Radius$ as the maximum manhattan distance a node can be from the player. $\forall v \in V: |P_x-v_x|+|P_y-v_y| \leq Radius$
* For some $v \in V:$ define $v_x$ as the x-coordinate of the node $v$ and $v_y$ as the y-coordinate of the node $v$
* Define $v \in V: Position(v)=(v_x,v_y)$ coordinate of that node within the graph.
* Define $Depth(v)=|v_x|+|v_y|$
* 1) Given a node $v \in V:Position(v)=(x,y)$
* 2) Generate edges in random directions to $(x \pm 1, y \pm 1)$
* 3) Mark $v$ as "explored" and add all nodes $v'$ to a queue $F$.
    * $v'$ fulfills:
      * $|P_x-v'_x|+|P_y-v'_y| \leq Radius$
      * $<v,v'>\in E$ 
* 4) Pop $F$ to get $v_{new}$ and repeat the algorithm on $v_{new}$ until $F$ is empty.