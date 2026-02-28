using Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories
{
    public class GeminiPromptsRepository : IGeminiPromptsRepository
    {
        MyShopContext _context;

        public GeminiPromptsRepository(MyShopContext shopContext)
        {
            _context = shopContext;
        }

        async public Task<GeminiPrompt> AddPromptAsync(GeminiPrompt prompt)
        {
            await _context.GeminiPrompts.AddAsync(prompt);

            await _context.SaveChangesAsync();
            return prompt;
        }

        async public Task UpdatePromptAsync(long id, GeminiPrompt prompt)
        {
            _context.GeminiPrompts.Update(prompt);
            await _context.SaveChangesAsync();
        }

        async public Task<GeminiPrompt?> GetByIDPromptAsync(long id)
        {
            return await _context.GeminiPrompts.FirstOrDefaultAsync(x => x.PromptId == id);
        }

        async public Task DeletePromptAsync(long id)
        {
            GeminiPrompt prompt = await _context.GeminiPrompts.FirstOrDefaultAsync(x => x.PromptId == id);
            _context.GeminiPrompts.Remove(prompt);
            await _context.SaveChangesAsync();
        }
    }
}
