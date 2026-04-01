// FILE: Services/Interfaces/ITaskCommentService.cs
// STATUS: NEW
// CHANGES: Created for fully exposing TaskComment entity (#21)

using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.DTOs.TaskComments;

namespace Maui_Task.Web.Services.Interfaces
{
    public interface ITaskCommentService
    {
        Task<IEnumerable<TaskCommentDto>> GetCommentsAsync(int taskId);
        Task<TaskCommentDto> GetCommentByIdAsync(int commentId);
        Task<TaskCommentDto> CreateCommentAsync(int userId, int taskId, CreateTaskCommentRequest request);
        Task<TaskCommentDto> UpdateCommentAsync(int userId, int commentId, UpdateTaskCommentRequest request);
        Task DeleteCommentAsync(int userId, int commentId);
    }
}
