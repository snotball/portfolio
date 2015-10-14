use timereg
go

drop table registration
drop table assignment
drop table project
drop table employee
drop table department
go

create table department
(
id int identity(1,1) primary key,
name varchar(64) not null
)

create table employee
(
id int identity(1,1) primary key,
name varchar(32) not null,
birthday datetime not null,
departmentid int not null foreign key references department
)

create table project
(
id int identity(1,1) primary key,
name varchar(64) not null,
startdate datetime not null,
estimatedhours int not null,
departmentid int not null foreign key references department
)

create table assignment
(
id int identity(1,1) primary key,
estimatedhours int not null,
employeeid int not null foreign key references employee,
projectid int not null foreign key references project
)

create table registration
(
id int identity(1,1) primary key,
date datetime not null,
registeredhours int not null,
employeeid int not null foreign key references employee,
projectid int not null foreign key references project
)
go

-- PRELIMINARY DATA
insert into department values('Money Grubbers')
insert into department values('Code Monkeys')
insert into department values('Artsy Farts')
go

insert into employee values('Albert', '1960-02-05', 1)
insert into employee values('Benny', '1965-04-03', 2)
insert into employee values('Conrad', '1970-06-07', 2)
insert into employee values('Dennis', '1975-04-08', 3)
insert into employee values('Erik', '1980-09-10', 3)
insert into employee values('Finn', '1985-07-03', 3)
go

insert into project values('Money Grubbing Galore', '2010-07-10', 24, 1)
insert into project values('Cool Coding Bonanza', '2012-05-20', 48, 2)
insert into project values('CrapChat Programming', '2013-02-25', 32, 2)
go

insert into assignment values(24, 1, 1)
insert into assignment values(32, 2, 2)
insert into assignment values(16, 3, 2)
insert into assignment values(32, 3, 3)
go

insert into registration values('2010-07-10', 8, 1, 1)
insert into registration values('2010-07-11', 8, 1, 1)

insert into registration values('2012-05-20', 8, 2, 2)
insert into registration values('2012-05-21', 8, 2, 2)
insert into registration values('2012-05-22', 8, 2, 2)
insert into registration values('2012-05-23', 4, 2, 2)
insert into registration values('2012-05-20', 8, 3, 2)
insert into registration values('2012-05-21', 8, 3, 2)
insert into registration values('2012-05-22', 4, 3, 2)

insert into registration values('2013-02-25', 8, 3, 3)
go