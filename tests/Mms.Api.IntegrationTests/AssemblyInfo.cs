using Xunit;

// Mỗi TestApi khởi động một PostgreSQL container và chạy entry point của Mms.Api
// qua WebApplicationFactory. Chạy song song làm hai luồng cùng kích hoạt entry point
// và một bên nhận "The entry point exited without ever building an IHost".
[assembly: CollectionBehavior(DisableTestParallelization = true)]
