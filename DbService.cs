using _20150916_Day7.Model;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace _20150916_Day7.Services
{
    class DbService
    {
        private static DbService instance;
        public static DbService Instance
        {
            get
            {
                if (instance == null)
                    instance = new DbService();

                return instance;
            }
        }

        string conStr;
        DbProviderFactory dbFactory;
        DbConnection dbCon;

        private DbService()
        {
            conStr = @"Data Source=localhost; Initial Catalog=timereg; user id=sa; password=1234";
            //conStr = @"Data Source=localhost; user id=system; password=1234";

            dbFactory = DbProviderFactories.GetFactory("System.Data.SqlClient");
            //dbFactory = DbProviderFactories.GetFactory("System.Data.OracleClient");

            dbCon = dbFactory.CreateConnection();
            dbCon.ConnectionString = conStr;
        }

        public bool CheckConnection()
        {
            try
            {
                dbCon.Open();
                dbCon.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public List<Department> GetDepartments()
        {
            List<Department> lstDepartment = new List<Department>();

            DbCommand dbCmd = dbFactory.CreateCommand();
            dbCmd.Connection = dbCon;
            dbCmd.CommandText = "SELECT * FROM department ORDER BY name;";

            try
            {
                dbCon.Open();

                DbDataReader dbReader = dbCmd.ExecuteReader();
                while (dbReader.Read())
                {
                    Department d = new Department
                    {
                        Id = (int)dbReader["id"],
                        Name = (string)dbReader["name"]
                    };

                    lstDepartment.Add(d);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                dbCon.Close();
            }

            return lstDepartment;
        }

        public List<Project> GetProjects(Department d)
        {
            List<Project> lstProjects = new List<Project>();

            DbCommand dbCmd = dbFactory.CreateCommand();
            dbCmd.Connection = dbCon;
            dbCmd.CommandText = "SELECT * FROM project WHERE departmentid = @did ORDER BY name;";
            dbCmd.AddParameterWithValue("@did", d.Id);

            try
            {
                dbCon.Open();

                DbDataReader dbReader = dbCmd.ExecuteReader();
                while (dbReader.Read())
                {
                    Project p = new Project
                    {
                        Id = (int)dbReader["id"],
                        Name = (string)dbReader["name"],
                        StartDate = (DateTime)dbReader["startdate"],
                        EstimatedHours = (int)dbReader["estimatedhours"],
                        DepartmentId = (int)dbReader["departmentid"]
                    };

                    lstProjects.Add(p);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                dbCon.Close();
            }

            return lstProjects;
        }

        public List<Employee> GetAssignedEmployees(Project p)
        {
            List<Employee> lstEmployees = new List<Employee>();

            DbCommand dbCmd = dbFactory.CreateCommand();
            dbCmd.Connection = dbCon;
            dbCmd.CommandText = "SELECT e.id, e.name, e.birthday, e.departmentid";
            dbCmd.CommandText += " FROM assignment a JOIN employee e ON a.employeeid = e.id";
            dbCmd.CommandText += " WHERE a.projectid = @pid";
            dbCmd.CommandText += " ORDER BY e.name;";
            dbCmd.AddParameterWithValue("@pid", p.Id);

            try
            {
                dbCon.Open();

                DbDataReader dbReader = dbCmd.ExecuteReader();
                while (dbReader.Read())
                {
                    Employee e = new Employee
                    {
                        Id = (int)dbReader["id"],
                        Name = (string)dbReader["name"],
                        Birthday = (DateTime)dbReader["birthday"],
                        DepartmentId = (int)dbReader["departmentid"]
                    };

                    lstEmployees.Add(e);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                dbCon.Close();
            }

            return lstEmployees;
        }

        public List<Registration> GetRegistrations(Employee e, Project p)
        {
            List<Registration> lstReg = new List<Registration>();

            DbCommand dbCmd = dbFactory.CreateCommand();
            dbCmd.Connection = dbCon;
            dbCmd.CommandText = "SELECT * FROM registration";
            dbCmd.CommandText += " WHERE employeeid = @eid AND projectid = @pid";
            dbCmd.CommandText += " ORDER BY date;";
            dbCmd.AddParameterWithValue("@eid", e.Id);
            dbCmd.AddParameterWithValue("@pid", p.Id);

            try
            {
                dbCon.Open();

                DbDataReader dbReader = dbCmd.ExecuteReader();
                while (dbReader.Read())
                {
                    Registration r = new Registration
                    {
                        Id = (int)dbReader["id"],
                        Date = (DateTime)dbReader["date"],
                        RegisteredHours = (int)dbReader["registeredhours"],
                        EmployeeId = (int)dbReader["employeeid"],
                        ProjectId = (int)dbReader["projectid"]
                    };

                    lstReg.Add(r);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                dbCon.Close();
            }

            return lstReg;
        }

        public void InsertProject(Project p)
        {
            DbCommand dbCmd = dbFactory.CreateCommand();
            dbCmd.Connection = dbCon;
            dbCmd.CommandText = "INSERT INTO project VALUES(@name, @startdate, @estimatedhours, @departmentid);";
            dbCmd.AddParameterWithValue("@name", p.Name);
            dbCmd.AddParameterWithValue("@startdate", p.StartDate.ToString("yyyy-MM-dd"));
            dbCmd.AddParameterWithValue("@estimatedhours", p.EstimatedHours);
            dbCmd.AddParameterWithValue("@departmentid", p.DepartmentId);

            try
            {
                dbCon.Open();
                dbCmd.ExecuteNonQuery();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                dbCon.Close();
            }
        }

        public void UpdateProject(Project p)
        {
            DbCommand dbCmd = dbFactory.CreateCommand();
            dbCmd.Connection = dbCon;
            dbCmd.CommandText = "UPDATE project";
            dbCmd.CommandText += " SET name = @name, startdate = @startdate, estimatedhours = @estimatedhours";
            dbCmd.CommandText += " WHERE id = @id;";
            dbCmd.AddParameterWithValue("@name", p.Name);
            dbCmd.AddParameterWithValue("@startdate", p.StartDate.ToString("yyyy-MM-dd"));
            dbCmd.AddParameterWithValue("@estimatedHours", p.EstimatedHours);
            dbCmd.AddParameterWithValue("@id", p.Id);

            try
            {
                dbCon.Open();
                dbCmd.ExecuteNonQuery();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                dbCon.Close();
            }
        }

        public void DeleteProject(Project p)
        {
            DbCommand dbCmd = dbFactory.CreateCommand();
            dbCmd.Connection = dbCon;
            dbCmd.CommandText = "DELETE FROM assignment WHERE projectid = @id;";
            dbCmd.CommandText += " DELETE FROM registration WHERE projectid = @id;";
            dbCmd.CommandText += " DELETE FROM project WHERE id = @id;";
            dbCmd.AddParameterWithValue("@id", p.Id);

            try
            {
                dbCon.Open();
                dbCmd.ExecuteNonQuery();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                dbCon.Close();
            }
        }

        public List<Employee> GetUnassignedDepartmentEmployees(Department d, Project p)
        {
            List<Employee> lstEmp = new List<Employee>();

            DbCommand dbCmd = dbFactory.CreateCommand();
            dbCmd.Connection = dbCon;
            dbCmd.CommandText = "SELECT * FROM employee e";
            dbCmd.CommandText += " WHERE e.departmentid = @departmentid";
            dbCmd.CommandText += " AND NOT EXISTS (";
            dbCmd.CommandText += "SELECT * FROM assignment a";
            dbCmd.CommandText += " WHERE e.id = a.employeeid";
            dbCmd.CommandText += " AND a.projectid = @projectid)";
            dbCmd.CommandText += " ORDER BY e.name;";
            dbCmd.AddParameterWithValue("@departmentid", d.Id);
            dbCmd.AddParameterWithValue("@projectid", p.Id);

            try
            {
                dbCon.Open();

                DbDataReader dbReader = dbCmd.ExecuteReader();
                while (dbReader.Read())
                {
                    Employee e = new Employee
                    {
                        Id = (int)dbReader["id"],
                        Name = (string)dbReader["name"],
                        Birthday = (DateTime)dbReader["birthday"],
                        DepartmentId = (int)dbReader["departmentid"]
                    };

                    lstEmp.Add(e);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                dbCon.Close();
            }

            return lstEmp;
        }

        public void InsertAssignment(Assignment a)
        {
            DbCommand dbCmd = dbFactory.CreateCommand();
            dbCmd.Connection = dbCon;
            dbCmd.CommandText = "INSERT INTO assignment VALUES(@estimatedhours, @employeeid, @projectid);";
            dbCmd.AddParameterWithValue("@estimatedhours", a.EstimatedHours);
            dbCmd.AddParameterWithValue("@employeeid", a.EmployeeId);
            dbCmd.AddParameterWithValue("@projectid", a.ProjectId);

            try
            {
                dbCon.Open();
                dbCmd.ExecuteNonQuery();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                dbCon.Close();
            }
        }
    }
}
