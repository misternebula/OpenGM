using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGM.VirtualMachine.BuiltInFunctions
{
    public static class BufferFunctions
    {
	    [GMLFunction("buffer_create")]
		public static object buffer_create(object?[] args)
	    {
		    var size = args[0].Conv<int>();
		    var type = (BufferType)args[1].Conv<int>();
		    var alignment = args[2].Conv<int>();

		    return BufferManager.CreateBuffer(size, type, alignment);
	    }

	    [GMLFunction("buffer_delete")]
		public static object? buffer_delete(object?[] args)
	    {
		    var bufferIndex = args[0].Conv<int>();

		    var buffer = BufferManager.Buffers[bufferIndex];
		    buffer.Data = null!; // why

		    BufferManager.Buffers.Remove(bufferIndex);

		    return null;
	    }

		// buffer_write

		[GMLFunction("buffer_read")]
		public static object buffer_read(object?[] args)
		{
			var bufferIndex = args[0].Conv<int>();
			var type = args[1].Conv<int>();
			return BufferManager.ReadBuffer(bufferIndex, (BufferDataType)type);
		}

		// buffer_poke
		// buffer_peek
		// buffer_seek
		// buffer_save
		// buffer_save_ext

		[GMLFunction("buffer_load")]
		public static object buffer_load(object?[] args)
		{
			var filename = args[0].Conv<string>();
			return BufferManager.LoadBuffer(filename);
		}

		// buffer_load_ext
		// buffer_load_partial
		// buffer_save_async
		// buffer_load_async
		// buffer_async_group_begin
		// buffer_async_group_end
		// buffer_async_group_option
		// buffer_copy
		// buffer_exists
		// buffer_get_type
		// buffer_get_alignment
		// buffer_fill
		// buffer_get_size
		// buffer_tell
		// buffer_resize
		// buffer_md5
		// buffer_sha1
		// buffer_crc32
		// buffer_base64_encode
		// buffer_base64_decode
		// buffer_base64_decode_ext
		// buffer_sizeof
		// buffer_get_address
		// buffer_get_surface

		[GMLFunction("buffer_set_surface")]
		public static object? buffer_set_surface(object?[] args)
		{
			var buffer = args[0].Conv<int>();
			var surface = args[1].Conv<int>();
			var offset = args[2].Conv<int>();

			BufferManager.BufferSetSurface(buffer, surface, offset);

			return null;
		}

		// buffer_set_used_size
		// buffer_create_from_vertex_buffer
		// buffer_create_from_vertex_buffer_ext
		// buffer_copy_from_vertex_buffer
		// buffer_compress
		// buffer_decompress
	}
}
