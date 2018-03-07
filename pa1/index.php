<?php include("players.php"); ?>		


<?php 		

// Use this players to do most of the work
$data = new MySQL_DataMapper();

$allPlayers = $data->getAllPlayerNames();


if (isset($_GET['name'])) {

    $searchedName = $_GET["name"];

    if ($searchedName != "") {
        $searchedNameLowerCase = strtolower($searchedName);

        $shortest = -1;
        $closestPlayerMatch = "";

        foreach($allPlayers as $value) {
            $valueLowerCase = strtolower($value);
            // keep in mind value is the original name in DB
            $lev = levenshtein($searchedNameLowerCase, $valueLowerCase);

            // exact match check
            if ($lev == 0) {
                printPlayer($value, $data);
                //$playerData = $data->getPlayerByName($name);
                //echo json_encode($playerData);

                break;
            } 
            /*
            else if (strpos($valueLowerCase, $searchedNameLowerCase) !== false) { // checking if it is contained
                printPlayer($value, $data);
            } else if ($lev <= $shortest || $shortest < 0) {
                $closest = $value;
                $shortest = $lev;
                $closestPlayerMatch = $value;
            }
            */
        }

        //printPlayer($closestPlayerMatch, $data);
    }

    }
    


function printPlayer($name, $data) {

    $playerData = $data->getPlayerByName($name);
    // print_r($playerData);
    // do some echos here
    /*
    echo '<div class="container">';
    echo "<br>";
    echo "Name: " . $playerData["Name"];
    echo "<br>";
    echo "Team: " . $playerData["Team"];
    echo "<br>";
    echo "Games Played: " . $playerData["GP"];
    echo "<br>";
    echo "Avg Minutes Played: " . $playerData["Min"];
    echo "<br>";
    echo "<br>";    
    echo "Missed FG: " . $playerData["M-FG"];
    echo "<br>";
    echo "Attempted FG: " . $playerData["A-FG"];
    echo "<br>";
    echo "% Made FG: " . $playerData["Pct-FG"];
    echo "<br>";
    echo "<br>";    
    echo "Missed 3PT: " . $playerData["M-3PT"];
    echo "<br>";
    echo "Attempted 3PT: " . $playerData["A-3PT"];
    echo "<br>";
    echo "% Made 3PT: " . $playerData["Pct-3PT"];
    echo "<br>";
    echo "<br>";    
    echo "Missed FT: " . $playerData["M-FT"];
    echo "<br>";
    echo "Attempted FT: " . $playerData["A-FT"];
    echo "<br>";
    echo "% Made FT: " . $playerData["Pct-FT"];
    echo "<br>";
    echo "<br>";    
    echo "Offensive Rb: " . $playerData["Off-Rb"];
    echo "<br>";
    echo "Defensive Rb: " . $playerData["Def-Rb"];
    echo "<br>";
    echo "Total Avg Rb: " . $playerData["Tot-Rb"];
    echo "<br>";
    echo "<br>";    
    echo "Ast: " . $playerData["Ast"];
    echo "<br>";
    echo "TO: " . $playerData["TO"];
    echo "<br>";
    echo "Stl: " . $playerData["Stl"];
    echo "<br>";
    echo "Blk: " . $playerData["Blk"];
    echo "<br>";
    echo "PF: " . $playerData["PF"];
    echo "<br>";
    echo "PPG: " . $playerData["PPG"];
    echo "<br>";

    echo "</div>";
    */

    header( "Content-type: application/javascript" );
    // echo "here";
    echo $_GET['callback'] . "(" . json_encode($playerData) . ");";
}

?>