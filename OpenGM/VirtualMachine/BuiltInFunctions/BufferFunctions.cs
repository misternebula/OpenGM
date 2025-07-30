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

        [GMLFunction("buffer_write")]
        public static object buffer_write(object?[] args)
        {
            var bufferIndex = args[0].Conv<int>();
            var type = args[1].Conv<int>();
            var value = args[2]!;

            if (BufferManager.WriteBuffer(bufferIndex, value, (BufferDataType)type) is not null)
            {
                return -1;
            }

            return 0;
        }

        [GMLFunction("buffer_read")]
        public static object buffer_read(object?[] args)
        {
            var bufferIndex = args[0].Conv<int>();
            var type = args[1].Conv<int>();
            return BufferManager.ReadBuffer(bufferIndex, (BufferDataType)type);
        }

        // buffer_poke
        // buffer_peek

        [GMLFunction("buffer_seek")]
        public static object buffer_seek(object?[] args)
        {
            var bufferIndex = args[0].Conv<int>();
            var seekType = (BufferSeek)args[1].Conv<int>();
            var offset = args[2].Conv<int>();

            var buffer = BufferManager.Buffers[bufferIndex];
            buffer.Seek(seekType, offset);

            return buffer.BufferIndex;
        }

        [GMLFunction("buffer_save")]
        public static object? buffer_save(object?[] args)
        {
            var bufferIndex = args[0].Conv<int>();
            var filename = args[1].Conv<string>();
            BufferManager.SaveBuffer(bufferIndex, filename);
            return null;
        }

        // buffer_save_ext

        [GMLFunction("buffer_load")]
        public static object buffer_load(object?[] args)
        {
            var filename = args[0].Conv<string>();
            return BufferManager.LoadBuffer(filename);
        }

        // buffer_load_ext
        // buffer_load_partial

        [GMLFunction("buffer_save_async")]
        public static object? buffer_save_async(object?[] args)
        {
            var bufferIndex = args[0].Conv<int>();
            var filename = args[1].Conv<string>();
            var offset = args[2].Conv<int>();
            var size = args[3].Conv<int>();

            var buffer = BufferManager.Buffers[bufferIndex];

            var request = new BufferAsyncWriteRequest(buffer, filename, offset, size);
            request.Group = BufferManager.AsyncGroup;
            request.Instance = VMExecutor.Self.GMSelf;

            if (BufferManager.AsyncGroup is not null)
            {
                BufferManager.AsyncGroup.Requests.Add(request);
            }
            else
            {
                _ = request.Start();
            }

            return request.Id;
        }

        [GMLFunction("buffer_load_async")]
        public static object? buffer_load_async(object?[] args)
        {
            var bufferIndex = args[0].Conv<int>();
            var filename = args[1].Conv<string>();
            var offset = args[2].Conv<int>();
            var size = args[3].Conv<int>();

            var buffer = BufferManager.Buffers[bufferIndex];

            var request = new BufferAsyncReadRequest(buffer, filename, offset, size);
            request.Group = BufferManager.AsyncGroup;
            request.Instance = VMExecutor.Self.GMSelf;

            if (BufferManager.AsyncGroup is not null)
            {
                BufferManager.AsyncGroup.Requests.Add(request);
            }
            else
            {
                _ = request.Start();
            }

            return request.Id;
        }
        
        [GMLFunction("buffer_async_group_begin")]
        public static object? buffer_async_group_begin(object?[] args)
        {
            var name = args[0].Conv<string>();

            BufferManager.AsyncGroup = new(name, VMExecutor.Self.GMSelf);

            return null;
        }

        [GMLFunction("buffer_async_group_end")]
        public static object? buffer_async_group_end(object?[] args)
        {
            if (BufferManager.AsyncGroup is null)
            {
                throw new InvalidOperationException("Tried to end non-existent buffer async group");
            }

            var group = BufferManager.AsyncGroup;
            BufferManager.AsyncGroup = null;

            _ = group.StartAll();

            return group.Id;
        }

        // buffer_async_group_option
        // buffer_copy
        // buffer_exists
        // buffer_get_type
        // buffer_get_alignment
        // buffer_fill

        [GMLFunction("buffer_get_size")]
        public static object? buffer_get_size(object?[] args)
        {
            var index = args[0].Conv<int>();

            if (!BufferManager.Buffers.TryGetValue(index, out var buffer))
            {
                return -1; // TODO: HTML returns eBuffer_UnknownBuffer
            }

            return buffer.Size;
        }

        // buffer_tell

        [GMLFunction("buffer_resize")]
        public static object buffer_resize(object?[] args)
        {
            var bufferIndex = args[0].Conv<int>();
            var size = args[1].Conv<int>();

            var buffer = BufferManager.Buffers[bufferIndex];
            buffer.Resize(size);

            return buffer.BufferIndex;
        }

        [GMLFunction("buffer_md5")]
        public static object? buffer_md5(object?[] args)
        {
            var buffer = args[0].Conv<int>();
            var offset = args[1].Conv<int>();
            var size = args[2].Conv<int>();

            if (!BufferManager.Buffers.TryGetValue(buffer, out var b))
            {
                return -1; // TODO: HTML returns eBuffer_UnknownBuffer
            }

            return b.MD5(offset, size);
        }

        // buffer_sha1
        // buffer_crc32
        // buffer_base64_encode
        // buffer_base64_decode
        // buffer_base64_decode_ext
        // buffer_sizeof
        // buffer_get_address

        [GMLFunction("buffer_get_surface")]
        public static object? buffer_get_surface(object?[] args)
        {
            var buffer = args[0].Conv<int>();
            var surface = args[1].Conv<int>();
            var offset = args[2].Conv<int>();

            BufferManager.BufferGetSurface(buffer, surface, offset);

            return null;
        }

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
