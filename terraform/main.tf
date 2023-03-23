resource "aws_vpc" "QA-LATAM-IMAGINE-VPC" {
  cidr_block           = "10.121.0.0/16"
  enable_dns_hostnames = true

  tags = {
    Name = "QA-LATAM-IMAGINE-VPC"
  }
}


resource "aws_subnet" "QA-LATAM-IMAGINE-X-SUBNET-204" {
  vpc_id                  = aws_vpc.QA-LATAM-IMAGINE-VPC.id
  cidr_block              = "10.121.204.0/24"
  availability_zone       = "us-east-1a"
  map_public_ip_on_launch = true

  tags = {
    Name = "QA-LATAM-IMAGINE-X-SUBNET-204"
  }
}

resource "aws_subnet" "QA-LATAM-IMAGINE-Y-SUBNET-214" {
  vpc_id                  = aws_vpc.QA-LATAM-IMAGINE-VPC.id
  cidr_block              = "10.121.214.0/24"
  availability_zone       = "us-east-1c"
  map_public_ip_on_launch = true

  tags = {
    Name = "QA-LATAM-IMAGINE-Y-SUBNET-214"
  }
}

resource "aws_internet_gateway" "QA-LATAM-IMAGINE-IGW" {
  vpc_id = aws_vpc.QA-LATAM-IMAGINE-VPC.id

  tags = {
    Name = "QA-LATAM-IMAGINE-IGW"
  }
}

resource "aws_route_table" "QA-LATAM-RTB-Public" {
  vpc_id = aws_vpc.QA-LATAM-IMAGINE-VPC.id

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.QA-LATAM-IMAGINE-IGW.id
  }
  tags = {
    Name = "QA-LATAM-IMAGINE-RTB1-Public"
  }
}

/*resource "aws_route_table" "QA-LATAM-RTB-Private" {
  vpc_id = aws_vpc.QA-LATAM-IMAGINE-VPC.id
}
*/

resource "aws_route_table_association" "public_access_204_assoc" {
subnet_id = aws_subnet.QA-LATAM-IMAGINE-X-SUBNET-204.id
route_table_id = aws_route_table.QA-LATAM-RTB-Public.id
}

resource "aws_route_table_association" "public_access_214_assoc" {
subnet_id = aws_subnet.QA-LATAM-IMAGINE-Y-SUBNET-214.id
route_table_id = aws_route_table.QA-LATAM-RTB-Public.id
}




/*#######################################################################################################*/




resource "aws_iam_role" "LatamImagineHaProxyRole" {
  name = "LatamImagineHaProxyRole"
  path = "/"
  assume_role_policy = jsonencode(
    {
      "Version" : "2012-10-17",
      "Statement" : [
        {
          "Effect" : "Allow",
          "Principal" : {
            "Service" : "ec2.amazonaws.com"
          },
          "Action" : "sts:AssumeRole"
        }
      ]
  })
}

resource "aws_iam_policy" "LatamImagineHaProxyPolicy" {
  name        = "LatamImagineHaProxyPolicy"
  path        = "/"
  description = "Policy applied to all HA Proxy machines so that they have permissions to access secondary ip."

  policy = jsonencode({
    "Version" : "2012-10-17",
    "Statement" : [
      {
        "Action" : [
          "ec2:DescribeInstances",
          "ec2:AssignPrivateIpAddresses",
          "ec2:UnassignPrivateIpAddresses"
        ],
        "Effect" : "Allow",
        "Resource" : "*",
        "Sid" : "VisualEditor1"
      }
    ]
  })
}

/***********************************************************************************/

resource "aws_iam_role_policy_attachment" "LatamImagineHaProxyPolicyRoleAttach" {
  role       = aws_iam_role.LatamImagineHaProxyRole.name
  policy_arn = aws_iam_policy.LatamImagineHaProxyPolicy.arn
}

resource "aws_iam_instance_profile" "LatamImagineHaProxyPolicyRole" {
  name = "LatamImagineHaProxyPolicyRole"
  role = aws_iam_role.LatamImagineHaProxyRole.name
}



/*#######################################################################################################*/

resource "aws_security_group" "HAPROXY-SG" {
  name        = "HAPROXY-SG"
  description = "Allow all inbound and outbound traffic"
  vpc_id      = aws_vpc.QA-LATAM-IMAGINE-VPC.id

  ingress {
    description      = "Allow all incoming"
    from_port        = 0
    to_port          = 0
    protocol         = "-1"
    cidr_blocks      = ["0.0.0.0/0"]
    ipv6_cidr_blocks = ["::/0"]
  }

  egress {
    description      = "Allow all outgoing"
    from_port        = 0
    to_port          = 0
    protocol         = "-1"
    cidr_blocks      = ["0.0.0.0/0"]
    ipv6_cidr_blocks = ["::/0"]
  }

  tags = {
    Name = "Allow all inbound and outbound traffic"
  }
}

resource "aws_instance" "HAPROXY-X1" {
  ami                  = "ami-0c4ecc76a3575e53b"
  private_ip           = "10.121.204.113"
  subnet_id            = aws_subnet.QA-LATAM-IMAGINE-X-SUBNET-204.id
  vpc_security_group_ids = [aws_security_group.HAPROXY-SG.id]
  instance_type        = "c5.2xlarge"
  availability_zone    = "us-east-1a"
  iam_instance_profile = aws_iam_instance_profile.LatamImagineHaProxyPolicyRole.name
  key_name             = "LATAM_QA"

  tags = {
    Name = "QA-LATAM-HAPROXY-X1"
  }
}

resource "aws_instance" "HAPROXY-X2" {
  ami                  = "ami-0dcd4959f268d0b7d"
  private_ip           = "10.121.204.123"
  subnet_id            = aws_subnet.QA-LATAM-IMAGINE-X-SUBNET-204.id
  vpc_security_group_ids = [aws_security_group.HAPROXY-SG.id]
  instance_type        = "c5.2xlarge"
  availability_zone    = "us-east-1a"
  iam_instance_profile = aws_iam_instance_profile.LatamImagineHaProxyPolicyRole.name
  key_name             = "LATAM_QA"

  tags = {
    Name = "QA-LATAM-HAPROXY-X2"
  }
}

resource "aws_instance" "HAPROXY-X3" {
  ami                  = "ami-027d724b76b108757"
  private_ip           = "10.121.204.133"
  subnet_id            = aws_subnet.QA-LATAM-IMAGINE-X-SUBNET-204.id
  vpc_security_group_ids = [aws_security_group.HAPROXY-SG.id]
  instance_type        = "c5.2xlarge"
  availability_zone    = "us-east-1a"
  iam_instance_profile = aws_iam_instance_profile.LatamImagineHaProxyPolicyRole.name
  key_name             = "LATAM_QA"

  tags = {
    Name = "QA-LATAM-HAPROXY-X3"
  }
}

resource "aws_instance" "HAPROXY-Y1" {
  ami                  = "ami-0aa61bfb1900af895"
  private_ip           = "10.121.214.113"
  subnet_id            = aws_subnet.QA-LATAM-IMAGINE-Y-SUBNET-214.id
  vpc_security_group_ids = [aws_security_group.HAPROXY-SG.id]
  instance_type        = "c5.2xlarge"
  availability_zone    = "us-east-1c"
  iam_instance_profile = aws_iam_instance_profile.LatamImagineHaProxyPolicyRole.name
  key_name             = "LATAM_QA"

  tags = {
    Name = "QA-LATAM-HAPROXY-Y1"
  }
}

resource "aws_instance" "HAPROXY-Y2" {
  ami                  = "ami-0cd6f98605e923a12"
  private_ip           = "10.121.214.123"
  subnet_id            = aws_subnet.QA-LATAM-IMAGINE-Y-SUBNET-214.id
  vpc_security_group_ids = [aws_security_group.HAPROXY-SG.id]
  instance_type        = "c5.2xlarge"
  availability_zone    = "us-east-1c"
  iam_instance_profile = aws_iam_instance_profile.LatamImagineHaProxyPolicyRole.name
  key_name             = "LATAM_QA"

  tags = {
    Name = "QA-LATAM-HAPROXY-Y2"
  }
}

resource "aws_instance" "HAPROXY-Y3" {
  ami                  = "ami-02a23c085296b3da5"
  private_ip           = "10.121.214.133"
  subnet_id            = aws_subnet.QA-LATAM-IMAGINE-Y-SUBNET-214.id
  vpc_security_group_ids = [aws_security_group.HAPROXY-SG.id]
  instance_type        = "c5.2xlarge"
  availability_zone    = "us-east-1c"
  iam_instance_profile = aws_iam_instance_profile.LatamImagineHaProxyPolicyRole.name
  key_name             = "LATAM_QA"

  tags = {
    Name = "QA-LATAM-HAPROXY-Y3"
  }
}

/*####################################################PATRONI AND DB Clusters#############################################*/

resource "aws_instance" "LATAM-X-POSTGRESQL-01" {
  ami                  = "ami-03921e532b2a13ebb"
  private_ip           = "10.121.204.112"
  subnet_id            = aws_subnet.QA-LATAM-IMAGINE-X-SUBNET-204.id
  vpc_security_group_ids = [aws_security_group.HAPROXY-SG.id]
  instance_type        = "c5.2xlarge"
  availability_zone    = "us-east-1a"
  key_name             = "LATAM_QA"

  tags = {
    Name = "LATAM-X-POSTGRESQL-01"
  }
}

resource "aws_instance" "LATAM-X-POSTGRESQL-02" {
  ami                  = "ami-03921e532b2a13ebb"
  private_ip           = "10.121.204.122"
  subnet_id            = aws_subnet.QA-LATAM-IMAGINE-X-SUBNET-204.id
  vpc_security_group_ids = [aws_security_group.HAPROXY-SG.id]
  instance_type        = "c5.2xlarge"
  availability_zone    = "us-east-1a"
  key_name             = "LATAM_QA"

  tags = {
    Name = "LATAM-X-POSTGRESQL-02"
  }
}

resource "aws_instance" "LATAM-Y-POSTGRESQL-03" {
  ami                  = "ami-03921e532b2a13ebb"
  private_ip           = "10.121.214.112"
  subnet_id            = aws_subnet.QA-LATAM-IMAGINE-Y-SUBNET-214.id
  vpc_security_group_ids = [aws_security_group.HAPROXY-SG.id]
  instance_type        = "c5.2xlarge"
  availability_zone    = "us-east-1c"
  key_name             = "LATAM_QA"

  tags = {
    Name = "LATAM-Y-POSTGRESQL-03"
  }
}

/*##################################################DOCKER NODES############################################*/

resource "aws_instance" "QA-LATAM-DOCKER-X1" {
  ami                  = "ami-03179ad409aad2246"
  private_ip           = "10.121.204.111"
  subnet_id            = aws_subnet.QA-LATAM-IMAGINE-X-SUBNET-204.id
  vpc_security_group_ids = [aws_security_group.HAPROXY-SG.id]
  instance_type        = "c5.2xlarge"
  availability_zone    = "us-east-1a"
  key_name             = "LATAM_QA"

  tags = {
    Name = "QA-LATAM-DOCKER-X1"
  }
}

resource "aws_instance" "QA-LATAM-DOCKER-X2" {
  ami                  = "ami-025a9062e96a9f50b"
  private_ip           = "10.121.204.121"
  subnet_id            = aws_subnet.QA-LATAM-IMAGINE-X-SUBNET-204.id
  vpc_security_group_ids = [aws_security_group.HAPROXY-SG.id]
  instance_type        = "c5.2xlarge"
  availability_zone    = "us-east-1a"
  key_name             = "LATAM_QA"

  tags = {
    Name = "QA-LATAM-DOCKER-X2"
  }
}

resource "aws_instance" "QA-LATAM-DOCKER-X3" {
  ami                  = "ami-0559e7c82d50ca822"
  private_ip           = "10.121.204.131"
  subnet_id            = aws_subnet.QA-LATAM-IMAGINE-X-SUBNET-204.id
  vpc_security_group_ids = [aws_security_group.HAPROXY-SG.id]
  instance_type        = "c5.2xlarge"
  availability_zone    = "us-east-1a"
  key_name             = "LATAM_QA"

  tags = {
    Name = "QA-LATAM-DOCKER-X3"
  }
}

resource "aws_instance" "QA-LATAM-DOCKER-Y1" {
  ami                  = "ami-04e809ed611c1d399"
  private_ip           = "10.121.214.111"
  subnet_id            = aws_subnet.QA-LATAM-IMAGINE-Y-SUBNET-214.id
  vpc_security_group_ids = [aws_security_group.HAPROXY-SG.id]
  instance_type        = "c5.2xlarge"
  availability_zone    = "us-east-1c"
  key_name             = "LATAM_QA"

  tags = {
    Name = "QA-LATAM-DOCKER-Y1"
  }
}

resource "aws_instance" "QA-LATAM-DOCKER-Y2" {
  ami                  = "ami-0159254af97b99852"
  private_ip           = "10.121.214.121"
  subnet_id            = aws_subnet.QA-LATAM-IMAGINE-Y-SUBNET-214.id
  vpc_security_group_ids = [aws_security_group.HAPROXY-SG.id]
  instance_type        = "c5.2xlarge"
  availability_zone    = "us-east-1c"
  key_name             = "LATAM_QA"

  tags = {
    Name = "QA-LATAM-DOCKER-Y2"
  }
}

resource "aws_instance" "QA-LATAM-DOCKER-Y3" {
  ami                  = "ami-003800f9b631039ce"
  private_ip           = "10.121.214.131"
  subnet_id            = aws_subnet.QA-LATAM-IMAGINE-Y-SUBNET-214.id
  vpc_security_group_ids = [aws_security_group.HAPROXY-SG.id]
  instance_type        = "c5.2xlarge"
  availability_zone    = "us-east-1c"
  key_name             = "LATAM_QA"

  tags = {
    Name = "QA-LATAM-DOCKER-Y3"
  }
}