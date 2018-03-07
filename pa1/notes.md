Notes About PA-1

- put this at the end of the config file for phpMyAdmin

Pasted this at the end of the php config file:
https://stackoverflow.com/questions/4402482/using-phpmyadmin-to-administer-amazon-rds

## Edit in the:
config.inc.php

$i++;
$cfg['Servers'][$i]['host'] = 'mypadb.cbcseqedv1my.us-east-2.rds.amazonaws.com';
$cfg['Servers'][$i]['port'] = '3306';
$cfg['Servers'][$i]['verbose'] = 'mypadb';
$cfg['Servers'][$i]['connect_type'] = 'tcp';
$cfg['Servers'][$i]['extension'] = 'mysql';
$cfg['Servers'][$i]['compress'] = TRUE;
$cfg['Servers'][$i]['auth_type']     = 'config';
$cfg['Servers'][$i]['user']          = 'mypadb';
$cfg['Servers'][$i]['password']      = 'password';

## If my IP address changed then change the inbound security group settings for EC2 and RDS


Next we connect to the database using php:
https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/create_deploy_PHP.rds.html#php-rds-connect


How to compute and do search:
https://stackoverflow.com/questions/6661530/php-multidimensional-array-search-by-value

## Making Two PHP Files
- One for the splash page and the other focuses on the database and delivering material to the splash page
- I made a player class that is defining players, which may be helpful down the road

Security Group for RDS: 	
sg-dc3edab7

Security Group for EC2:

Private IP for EC2:
172.31.43.47

### Was having trouble getting back into the dev environment, I fixed it by adding my IP to the RDS security group