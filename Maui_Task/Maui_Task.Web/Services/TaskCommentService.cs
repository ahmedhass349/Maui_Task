// FILE: Services/TaskCommentService.cs
// STATUS: NEW
// CHANGES: Created for fully exposing TaskComment entity (#21)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.DTOs.TaskComments;
using Maui_Task.Web.Repositories.Interfaces;
using Maui_Task.Web.Services.Interfaces;

namespace Maui_Task.Web.Services
{
    public class TaskCommentService : ITaskCommentService
    {
        private readonly ITaskCommentRepository _commentRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IMapper _mapper;

        public TaskCommentService(
            ITaskCommentRepository commentRepository,
            ITaskRepository taskRepository,
            IMapper mapper)
        {
            _commentRepository = commentRepository;
            _taskRepository = taskRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TaskCommentDto>> GetCommentsAsync(int taskId)
        {
            var taskExists = await _taskRepository.ExistsAsync(t => t.Id == taskId);
            if (!taskExists)
                throw new KeyNotFoundException($"Task with ID {taskId} not found.");

            var comments = await _commentRepository.GetByTaskIdAsync(taskId);
            return _mapper.Map<IEnumerable<TaskCommentDto>>(comments);
        }

        public async Task<TaskCommentDto> GetCommentByIdAsync(int commentId)
        {
            var comment = await _commentRepository.GetByIdWithAuthorAsync(commentId);
            if (comment == null)
                throw new KeyNotFoundException($"Comment with ID {commentId} not found.");

            return _mapper.Map<TaskCommentDto>(comment);
        }

        public async Task<TaskCommentDto> CreateCommentAsync(int userId, int taskId, CreateTaskCommentRequest request)
        {
            var taskExists = await _taskRepository.ExistsAsync(t => t.Id == taskId);
            if (!taskExists)
                throw new KeyNotFoundException($"Task with ID {taskId} not found.");

            var comment = new TaskComment
            {
                TaskId = taskId,
                AuthorId = userId,
                Body = request.Body,
                CreatedAt = DateTime.UtcNow
            };

            await _commentRepository.AddAsync(comment);
            await _commentRepository.SaveChangesAsync();

            var saved = await _commentRepository.GetByIdWithAuthorAsync(comment.Id);
            return _mapper.Map<TaskCommentDto>(saved!);
        }

        public async Task<TaskCommentDto> UpdateCommentAsync(int userId, int commentId, UpdateTaskCommentRequest request)
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);
            if (comment == null)
                throw new KeyNotFoundException($"Comment with ID {commentId} not found.");

            // Only the author can edit their comment
            if (comment.AuthorId != userId)
                throw new UnauthorizedAccessException("You do not have permission to edit this comment.");

            comment.Body = request.Body;

            _commentRepository.Update(comment);
            await _commentRepository.SaveChangesAsync();

            var saved = await _commentRepository.GetByIdWithAuthorAsync(comment.Id);
            return _mapper.Map<TaskCommentDto>(saved!);
        }

        public async Task DeleteCommentAsync(int userId, int commentId)
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);
            if (comment == null)
                throw new KeyNotFoundException($"Comment with ID {commentId} not found.");

            // Only the author can delete their comment
            if (comment.AuthorId != userId)
                throw new UnauthorizedAccessException("You do not have permission to delete this comment.");

            _commentRepository.Remove(comment);
            await _commentRepository.SaveChangesAsync();
        }
    }
}
