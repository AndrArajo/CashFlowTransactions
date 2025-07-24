using Grpc.Core;
using CashFlowTransactions.Application.DTOs;
using CashFlowTransactions.Domain.Interfaces;
using CashFlowTransactions.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Confluent.Kafka;
using Google.Protobuf.WellKnownTypes;

namespace CashFlowTransactions.GrpcService.Services
{
    public class TransactionService : GrpcService.TransactionService.TransactionServiceBase
    {

        private readonly ITransactionService _transactionService;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(ITransactionService transactionService, ILogger<TransactionService> logger)
        {
            _transactionService = transactionService;
            _logger = logger;

        }

        public override async Task<CreateTransactionResponse> RegisterTransaction(CreateTransactionRequest request, ServerCallContext context)
        {
            if (request is null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Requisição inválida"));
            }

            
            var dto = new CreateTransactionDto
            {
                Description = request.Description,
                Amount = (decimal) request.Amount,
                Type = (Domain.Enums.TransactionType)request.Type,
                Origin = request.Origin,
                TransactionDate = request.TransactionDate?.ToDateTime()
            };

            _logger.Log(LogLevel.Debug, dto.ToString());

            var (savedTransaction, messageId) = await _transactionService.RegisterAsync(dto);

            return new CreateTransactionResponse
            {
                TransactionId = savedTransaction.Id,
                MessageId     = messageId,
                Status        = "Transação registrada com sucesso"
            };
        }

        public override async Task<Transaction> GetTransactionById(GetTransactionByIdRequest request, ServerCallContext context)
        {
            var transaction = await _transactionService.GetByIdAsync(request.Id);

            if (transaction == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Item não encontrado."));

            var transactionDto = new Transaction
            {
                Id = transaction.Id,
                Description = transaction.Description,
                Amount = (double) transaction.Amount,
                Type = (TransactionType) transaction.Type,
                Origin = transaction.Origin,
                TransactionDate = transaction.TransactionDate.ToTimestamp(),
                CreatedAt = transaction.CreatedAt.ToTimestamp()
            };

            return transactionDto;
        }

        public override async Task<PaginatedTransactionsResponse> GetTransactions(GetTransactionsRequest request, ServerCallContext context)
        {
            int pageSize = request.PageSize > 100 ? 100 : request.PageSize;
            pageSize = pageSize <= 0 ? 10 : pageSize;
            int pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;

            var filter = new TransactionFilterDto
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                StartDate = request.StartDate.ToDateTime(),
                EndDate = request.EndDate.ToDateTime(),
                Type = (Domain.Enums.TransactionType) request.Type,
                MinAmount = (decimal)request.MinAmount,
                MaxAmount = (decimal) request.MaxAmount,
                Description = request.Description,
                Origin = request.Origin
            };

            var paginatedResult = await _transactionService.GetTransactionsAsync(filter);

            var grpcItems = paginatedResult.Items.Select(t => new Transaction
            {
                Id = t.Id,
                Description = t.Description,
                Amount = (double)t.Amount,
                Type = (TransactionType)t.Type,
                Origin = t.Origin,
                TransactionDate = t.TransactionDate.ToTimestamp(),
                CreatedAt = t.CreatedAt.ToTimestamp()
            });

            var response = new PaginatedTransactionsResponse
            {
                PageNumber = paginatedResult.PageNumber,
                PageSize = paginatedResult.PageSize,
                TotalCount = paginatedResult.TotalCount,
                TotalPages = paginatedResult.TotalPages,
                HasPreviousPage = paginatedResult.HasPreviousPage,
                HasNextPage = paginatedResult.HasNextPage
            };

            response.Items.AddRange(grpcItems);

            return response;
        }
    }
}
