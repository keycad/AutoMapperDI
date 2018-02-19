using System;
using System.Collections.Generic;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoMapperGistApi_Posted
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddTransient<Test>();
            services.AddScoped<IInjectedService, InjectedService>();

            // AutoMapper
            // Without AddAutoMapper: it can't inject the injected service in AfterMapAction
            // With it: it calls the wrong User ctor, with roles param
            //var mapper = new Mapper(CreateMapperConfiguration());
            //services.AddSingleton<IMapper>(mapper);
            services.AddAutoMapper(typeof(Startup));

            services.BuildServiceProvider().GetRequiredService<Test>().StartTest();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseMvc();
        }

        public MapperConfiguration CreateMapperConfiguration()
        {
            return new MapperConfiguration(cfg =>
            {
                cfg.DisableConstructorMapping();
                // Add all profiles in selected assemblies
                cfg.AddProfiles(typeof(Startup));
            });
        }
    }

    public class Test
    {
        IMapper Mapper;

        public Test(IMapper mapper)
        {
            Mapper = mapper;
        }

        public void StartTest()
        {
            var sourceRoleDTO = new RoleDTO() { Name = "TestRole" };
            var sourceUserDTO = new UserDTO
            {
                Name = "TestUser",
                Email = "mymail@mails.com",
                Roles = new List<RoleDTO>() { sourceRoleDTO }
            };

            var role = Mapper.Map<Role>(sourceRoleDTO);
            var user = Mapper.Map<User>(sourceUserDTO);
        }

    }

    public class MyProfile : Profile
    {
        public MyProfile()
        {
            CreateMap<RoleDTO, Role>();
            CreateMap<UserDTO, User>()
                .ForMember(entity => entity.Roles, opt => opt.Ignore())
                // With this line, it works, but it's boilerplate for every map
                //.ConstructUsing(src => new User())
                .AfterMap<AfterMapAction>();
        }
    }


    public class AfterMapAction : IMappingAction<UserDTO, User>
    {
        private readonly IInjectedService _injectedService;

        public AfterMapAction(IInjectedService injservice)
        {
            _injectedService = injservice;
        }

        public void Process(UserDTO dto, User user)
        {
            // Do something with dto, user and injected service
            user.Name = _injectedService.SomeMethod(dto.Name);
        }
    }


    public interface IInjectedService
    {
        string SomeMethod(string name);
    }

    public class InjectedService : IInjectedService
    {
        // This service injects a DBContext to retrieve some data from the database
        public InjectedService(/*DBContext DBContext*/) { }

        public string SomeMethod(string name)
        {
            return $"{name}2";
        }
    }

    public class RoleDTO
    {
        public string Name { get; set; }
        public RoleDTO() { }
    }

    public class UserDTO
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public ICollection<RoleDTO> Roles { get; set; }
        public UserDTO() { }
    }

    public class Role
    {
        public string Name { get; set; }
        public Role() { }
    }

    public class User
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public ICollection<Role> Roles { get; }
        public User()
        {
            Roles = new List<Role>();
        }

        public User(string name, string email, ICollection<Role> roles) : this()
        {
            // SHOULD NOT USE THIS CONSTRUCTOR!!!
            foreach (Role r in roles)
                Roles.Add(r);

            throw new Exception("Wrong constructor");
        }

    }
}