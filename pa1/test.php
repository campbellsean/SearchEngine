<?php 



    $username = "mypadb";
    $password = "password";	

        try {
            $dbConnection = new PDO('mysql:host=mypadb.cbcseqedv1my.us-east-2.rds.amazonaws.com;dbname=pa_1', $this->username, $this->password);
            $dbConnection->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
        
            $data = $dbConnection->query('SELECT * FROM Players');
            
        } catch(PDOException $e) {
            echo 'ERROR: ' . $e->getMessage();
        }

        $name = "Stephen Curry";
        foreach ($data as $key => $val) {
            if ($val['Name'] === $name) {
                // print_r($val);
                return $val;
            }
        }



        ?>