// FILE: Services/ProjectService.cs
// STATUS: UPDATED
// CHANGES: Added userId ownership checks to Update/Delete/ToggleStar (#2),
//          Fixed double-fetch pattern in Create/Update/ToggleStar (#13)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.DTOs.Projects;
using Maui_Task.Web.Repositories.Interfaces;
using Maui_Task.Web.Services.Interfaces;
using TaskStatus = Maui_Task.Shared.Data.Entities.TaskStatus;

namespace Maui_Task.Web.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IMapper _mapper;

        public ProjectService(IProjectRepository projectRepository, IMapper mapper)
        {
            _projectRepository = projectRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProjectDto>> GetUserProjectsAsync(int userId)
        {
            var projects = await _projectRepository.GetUserProjectsAsync(userId);
            return _mapper.Map<IEnumerable<ProjectDto>>(projects);
        }

        public async Task<ProjectDto> GetProjectByIdAsync(int projectId)
        {
            var project = await _projectRepository.GetProjectWithDetailsAsync(projectId);
            if (project == null)
                throw new KeyNotFoundException($"Project with ID {projectId} not found.");

            return _mapper.Map<ProjectDto>(project);
        }

        public async Task<ProjectDto> CreateProjectAsync(int userId, CreateProjectRequest request)
        {
            var project = new Project
            {
                Name = request.Name,
                Description = request.Description,
                Color = request.Color,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _projectRepository.AddAsync(project);
            await _projectRepository.SaveChangesAsync();

            // Fix #13: Re-fetch with details in one call instead of double-fetch
            var saved = await _projectRepository.GetProjectWithDetailsAsync(project.Id);
            return _mapper.Map<ProjectDto>(saved!);
        }

        public async Task<ProjectDto> UpdateProjectAsync(int userId, int projectId, UpdateProjectRequest request)
        {
            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project == null)
                throw new KeyNotFoundException($"Project with ID {projectId} not found.");

            // Ownership check (#2)
            if (project.OwnerId != userId)
                throw new UnauthorizedAccessException("You do not have permission to update this project.");

            project.Name = request.Name;
            project.Description = request.Description;
            project.Color = request.Color;

            _projectRepository.Update(project);
            await _projectRepository.SaveChangesAsync();

            // Fix #13: Single re-fetch
            var saved = await _projectRepository.GetProjectWithDetailsAsync(project.Id);
            return _mapper.Map<ProjectDto>(saved!);
        }

        public async Task DeleteProjectAsync(int userId, int projectId)
        {
            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project == null)
                throw new KeyNotFoundException($"Project with ID {projectId} not found.");

            // Ownership check (#2)
            if (project.OwnerId != userId)
                throw new UnauthorizedAccessException("You do not have permission to delete this project.");

            _projectRepository.Remove(project);
            await _projectRepository.SaveChangesAsync();
        }

        public async Task<ProjectDto> ToggleStarAsync(int userId, int projectId)
        {
            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project == null)
                throw new KeyNotFoundException($"Project with ID {projectId} not found.");

            // Ownership check (#2)
            if (project.OwnerId != userId)
                throw new UnauthorizedAccessException("You do not have permission to modify this project.");

            project.IsStarred = !project.IsStarred;

            _projectRepository.Update(project);
            await _projectRepository.SaveChangesAsync();

            // Fix #13: Single re-fetch
            var saved = await _projectRepository.GetProjectWithDetailsAsync(project.Id);
            return _mapper.Map<ProjectDto>(saved!);
        }

        public async Task<IEnumerable<ProjectMemberDto>> GetProjectMembersAsync(int projectId)
        {
            var project = await _projectRepository.GetProjectWithDetailsAsync(projectId);
            if (project == null)
                throw new KeyNotFoundException($"Project with ID {projectId} not found.");

            var members = project.Members?.Select(m => new ProjectMemberDto
            {
                UserId = m.UserId,
                UserName = m.User?.FullName ?? string.Empty,
                Email = m.User?.Email ?? string.Empty,
                AvatarUrl = m.User?.AvatarUrl,
                Role = m.Role.ToString()
            }).ToList() ?? new List<ProjectMemberDto>();

            return members;
        }
    }
}
