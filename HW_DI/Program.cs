using Microsoft.Extensions.Primitives;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var allServices = builder.Services;

builder.Services.AddSingleton<IUserRepository, UserRepository>();

var app = builder.Build();

app.MapWhen(
    context => context.Request.Path == "/",
    appBuilder => appBuilder.Run(async context =>
    {
        var users = context.RequestServices.GetService<IUserRepository>();
        var st = new StringBuilder();
        st.Append("<table><tr><th>Id</th><th>Name</th><th>Age</th><th>Age</th><th>Edit</th><th>Delete</th>");
        foreach(var item in users.GetAllUser())
        {
            st.Append($"<tr><td>{item.Id}</td><td>{item.Name}</td><td>{item.Age}</td>" +
                $"<td><a href='/getUser?id={item.Id}'>Get</a></td>" +
                $"<td><a href='/editUser?id={item.Id}'>Edit</a></td>" +
                $"<td><a href='/deleteUser?id={item.Id}'>Delete</a></td></tr>");     
        }
        st.Append("/<table>");
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync("<ul><li><a href=\"/\">Index page</a></li><li><a href=\"/addUser\">Add User</a></li></ul>" +
            "<div><h2>All users: </h2><div>" +
            "<div>" + st.ToString() + "</div>");
    })
    );

app.MapWhen(
    context => context.Request.Path == "/getUser" && context.Request.Method == "GET",
    appBuilder => appBuilder.Run(async context =>
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        var users = context.RequestServices.GetService<IUserRepository>();
        string strID = context.Request.Query["id"];
        int id = 0;
        if(int.TryParse(strID, out id))
        {
            await context.Response.WriteAsync("<ul><li><a href=\"/\">Index Page</a></li><li><a href=\"/addUser\">Add User</a></li></ul>" +
            "<div><h2>Current User: <h2></div>" +
            "<div>" + users.GetUser(id) + "</div>");
        }
        else
        {
            await context.Response.WriteAsync("<ul><li><a href=\"/\">Index Page</a></li><li><a href=\"/addUser\">Add User</a></li></ul>" +
                $"<div><h2>Cant find user with id: {strID}</h2></div>");
        }

    })
    );

app.MapWhen(
    context => context.Request.Path == "/addUser" && context.Request.Method == "GET",
    appBuilder => appBuilder.Run(async context =>
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync("""
            <h2>Add User</h2>
            <form action=/addUser method="post">
            <label for="name">Name:</label></br>
            <input type="text" id="name" name="Name" required><br>
            <label for="age">Age:</label><br>
            <input type="number" id="age" name="Age" required><br><br>
            <button type="submit">Add</button>
            </form>
            """);
    })
    );
app.MapWhen(
    context => context.Request.Path == "/addUser" && context.Request.Method == "POST",
    appBuilder => appBuilder.Run(async context =>
    {
        var requestName = context.Request.Form["name"];
        var requestAge = context.Request.Form["age"];

        if (string.IsNullOrEmpty(requestName) || string.IsNullOrEmpty(requestAge))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Name and age would be writing");
            return;
        }
        var users = context.RequestServices.GetService<IUserRepository>();

        users.AddUser(new User
        {
            Id = users.GetLastID() + 1,
            Name = requestName,
            Age = int.Parse(requestAge)
        });
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync("<ul><li><a href=\"/\">Index page</a></li><li><a href=\"/addUser\">Add User</a></li></ul><div>User successful added!</div>");
    })
    );

app.MapWhen(
    context => context.Request.Path == "/editUser" && context.Request.Method == "GET",
    appBuilder => appBuilder.Run(async context =>
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        var users = context.RequestServices.GetService<IUserRepository>();
        string strId = context.Request.Query["id"];
        if(int.TryParse(strId,out int id))
        {
            var user=  users.GetUser(id);
            if(user !=null)
            {
                await context.Response.WriteAsync($"""
            <h2>Edit User</h2>
            <form action=/editUser method="post">
                <input type="hidden" name="Id" value="{user.Id}" />
                <label for="name">Name:</label></br>
                <input type="text" id="name" name="Name" value="{user.Name}" required><br>
                <label for="age">Age:</label><br>
                <input type="number" id="name" name="Age" value="{user.Age}" required><br><br>
                <button type="submit">Add</button>
            </form>
            """);
            }
        }
    })
    );
app.MapWhen(
    context => context.Request.Path == "/editUser" && context.Request.Method == "POST",
    appBuilder => appBuilder.Run(async context =>
    {
        var users = context.RequestServices.GetService<IUserRepository>();
        var id = int.Parse(context.Request.Form["id"]);
        var name = context.Request.Form["Name"];
        var age = context.Request.Form["Age"];

        users.UpdateUser(new User
        {
            Id = id,
            Name = name,
            Age= int.Parse(age)
        });
    })
    );
app.MapWhen(
    context => context.Request.Path == "/deleteUser" && context.Request.Method == "POST",
    appBuilder => appBuilder.Run(async context =>
    {
        var users = context.RequestServices.GetService<IUserRepository>();
        var id = 0;
        var strId = context.Request.Query["id"];
        if(int.TryParse(strId, out id))
        {
            users.DeleteUser(id);
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(("<ul><li><a href=\"/\">Index page</a></li><li><a href=\"/addUser\">Add User</a></li></ul><div>User successful Deleted!</div>")
        }
        else
        {
            await context.Response.WriteAsync($"<div>Invalid user Id: {strId}</div>");
        }
    })
    );

app.Run();


public interface IUserRepository
{
    void AddUser(User user);
    void UpdateUser(User user);
    User GetUser(int id);
    void DeleteUser(int id);

    List<User> GetAllUser();
    int GetLastID();
}
public class UserRepository : IUserRepository
{
    private List<User> users;
    public UserRepository() => users = new List<User>(); 
    public void AddUser(User user)
    {
        users.Add(user);
    }
    public void UpdateUser(User user)
    {
        var currentUser = users.FirstOrDefault(e => e.Id == user.Id);
        if(currentUser != null)
        {
            currentUser.Name = user.Name;
            currentUser.Age = user.Age;
        }
    }
    public void DeleteUser(int id)
    {
        users = users.Where(e=>e.Id != id).ToList();
    }

    public User GetUser(int id)
    {
        return users.FirstOrDefault(e => e.Id == id);
    }
    public List<User> GetAllUser()
    {
        return users;
    }

    public int GetLastID()
    {
        return users.Count > 0 ? users[users.Count - 1].Id : 1;
    }
}


public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }

    public override string ToString()
    {
        return $"ID: {Id}, name: {Name}, age: {Age}";
    }
}
