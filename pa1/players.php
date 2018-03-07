<?php 

class MySQL_DataMapper
{

    private $username = "mypadb";
    private $password = "password";	
    

    public function getPlayerByName($name)
    {
        try {
            $dbConnection = new PDO('mysql:host=mypadb.cbcseqedv1my.us-east-2.rds.amazonaws.com;dbname=pa_1', $this->username, $this->password);
            $dbConnection->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
        
            $data = $dbConnection->query('SELECT * FROM Players');
            
        } catch(PDOException $e) {
            echo 'ERROR: ' . $e->getMessage();
        }
        foreach ($data as $key => $val) {
            if ($val['Name'] === $name) {
                // print_r($val);
                return $val;
            }
        }
    }

    public function getAllPlayerNames()
    {
        try {
            $dbConnection = new PDO('mysql:host=mypadb.cbcseqedv1my.us-east-2.rds.amazonaws.com;dbname=pa_1', $this->username, $this->password);
            $dbConnection->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
        
            $data = $dbConnection->query('SELECT * FROM Players');
            
        } catch(PDOException $e) {
            echo 'ERROR: ' . $e->getMessage();
        }
        $allPlayerNames = array();
        foreach ($data as $key => $val) {
                array_push($allPlayerNames, $val["Name"]);
        }
        return $allPlayerNames;
    }
}

?>