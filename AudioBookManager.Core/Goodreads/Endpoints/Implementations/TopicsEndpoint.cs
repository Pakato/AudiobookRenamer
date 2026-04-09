using Goodreads.Helpers;
using Goodreads.Http;
using Goodreads.Models.Request;
using Goodreads.Models.Response;
using RestSharp;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Goodreads.Clients
{
    internal sealed class TopicsEndpoint : Endpoint, IOAuthTopicsEndpoint
    {
        public TopicsEndpoint(IConnection connection)
            : base(connection)
        {
        }

        public async Task<Topic> GetInfo(long topicId)
        {
            var endpoint = $"topic/show?id={topicId}";
            return await Connection.ExecuteRequest<Topic>(endpoint, null, null, "topic").ConfigureAwait(false);
        }

        public async Task<PaginatedList<Topic>> GetTopics(
            long folderId,
            long groupId,
            int page = 1,
            GroupFolderSort sort = GroupFolderSort.Title,
            OrderInfo order = OrderInfo.Asc)
        {
            var endpoint = $"topic/group_folder/{folderId}";

            var parameters = new[]
            {
                 Parameter.CreateParameter("group_id", groupId, ParameterType.QueryString),
                 Parameter.CreateParameter("page", page, ParameterType.QueryString),
                 Parameter.CreateParameter(EnumHelpers.QueryParameterKey<GroupFolderSort>(), EnumHelpers.QueryParameterValue(sort), ParameterType.QueryString),
                 Parameter.CreateParameter(EnumHelpers.QueryParameterKey<OrderInfo>(), EnumHelpers.QueryParameterValue(order), ParameterType.QueryString)
            };

            return await Connection.ExecuteRequest<PaginatedList<Topic>>(endpoint, parameters, null, "group_folder/topics").ConfigureAwait(false);
        }

        public async Task<PaginatedList<Topic>> GetUnreadTopics(
            long groupId,
            bool viewed = false,
            int page = 1,
            GroupFolderSort sort = GroupFolderSort.Title,
            OrderInfo order = OrderInfo.Asc)
        {
            var endpoint = $"topic/unread_group/{groupId}";

            var parameters = new List<Parameter>
            {
                 Parameter.CreateParameter("page", page, ParameterType.QueryString),
                 Parameter.CreateParameter(EnumHelpers.QueryParameterKey<GroupFolderSort>(), EnumHelpers.QueryParameterValue(sort), ParameterType.QueryString),
                 Parameter.CreateParameter(EnumHelpers.QueryParameterKey<OrderInfo>(), EnumHelpers.QueryParameterValue(order), ParameterType.QueryString)
            };

            if (viewed)
            {
                parameters.Add(Parameter.CreateParameter("viewed", viewed, ParameterType.QueryString));
            }

            return await Connection.ExecuteRequest<PaginatedList<Topic>>(endpoint, parameters, null, "group_folder/topics").ConfigureAwait(false);
        }

        public async Task<Topic> CreateTopic(
            TopicSubjectType type,
            long subjectId,
            long? folderId,
            string title,
            bool isQuestion,
            string comment,
            bool addToUpdateFeed,
            bool needDigest)
        {
            var endpoint = "topic";

            var parameters = new List<Parameter>
            {
                 Parameter.CreateParameter(EnumHelpers.QueryParameterKey<TopicSubjectType>(), EnumHelpers.QueryParameterValue(type), ParameterType.QueryString),
                 Parameter.CreateParameter("topic[subject_id]", subjectId, ParameterType.QueryString),
                 Parameter.CreateParameter("topic[title]", title, ParameterType.QueryString),
                 Parameter.CreateParameter("topic[question_flag]", isQuestion ? "1" : "0", ParameterType.QueryString),
                 Parameter.CreateParameter("comment[body_usertext]", comment, ParameterType.QueryString)
            };

            if (folderId.HasValue)
            {
                parameters.Add(Parameter.CreateParameter("topic[folder_id]", folderId.Value, ParameterType.QueryString));
            }

            if (addToUpdateFeed)
            {
                parameters.Add(Parameter.CreateParameter("update_feed", "on", ParameterType.QueryString));
            }

            if (needDigest)
            {
                parameters.Add(Parameter.CreateParameter("digest", "on", ParameterType.QueryString));
            }

            return await Connection.ExecuteRequest<Topic>(endpoint, parameters, null, "topic", Method.Post);
        }
    }
}
